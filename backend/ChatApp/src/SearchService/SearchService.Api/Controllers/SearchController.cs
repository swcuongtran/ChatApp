using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using SearchService.Api.DbContexts;
using SearchService.Api.DTOs;
using SearchService.Api.Model;
using SearchService.Api.Services;
using System.Security.Claims;

namespace SearchService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly IElasticClient _client;
        private readonly IEmbeddingService _embeddingService;
        private readonly SearchDbContext _context;
        private readonly ILogger<SearchController> _logger;
        public SearchController(IElasticClient client, IEmbeddingService embeddingService, SearchDbContext searchDbContext, ILogger<SearchController> logger)
        {
            _embeddingService = embeddingService;
            _client = client;
            _logger = logger;
            _context = searchDbContext;
        }
        [HttpGet("search-context")]
        public async Task<IActionResult> SearchContext([FromQuery] string description, [FromQuery] string? conversationId)
        {
            if (string.IsNullOrWhiteSpace(description)) return BadRequest();

            // 1. Tiền xử lý chuỗi truy vấn
            var processedTerm = NormalizedQuery(description);

            // 2. Chuyển đổi truy vấn thành Vector
            var queryVector = await _embeddingService.GetEmbeddingAsync(processedTerm);
            if (queryVector.Length == 0) return Ok(Array.Empty<ContextSegmentDto>());

            // 3. TÌM KIẾM MỎ NEO (Hybrid Search: BM25 + Cosine Similarity)
            var anchorResponse = await _client.SearchAsync<MessageDoc>(s => s
                .Index("chat_messages")
                .Size(5) 
                .Query(q => q
                    .Bool(b => b
                        .Filter(f => !string.IsNullOrEmpty(conversationId)
                            ? f.Term(t => t.Field(f => f.ConversationId).Value(conversationId))
                            : f.MatchAll()
                        )
                        .Should(
                            // Điểm Lexical
                            sh => sh.Match(m => m
                                .Field(f => f.Content)
                                .Query(processedTerm)
                                .Boost(0.4)
                            ),
                            // Điểm Semantic
                            sh => sh.ScriptScore(ss => ss
                                .Query(qq => qq.Exists(e => e.Field(f => f.Embedding)))
                                .Script(sc => sc
                                    .Source("cosineSimilarity(params.qv, 'embedding') + 1.0")
                                    .Params(p => p.Add("qv", queryVector))
                                )
                                .Boost(0.6)
                            )
                        )
                        .MinimumShouldMatch(1)
                    )
                )
            );

            if (!anchorResponse.IsValid || !anchorResponse.Documents.Any())
                return Ok(new List<ContextSegmentDto>());

            var segments = new List<ContextSegmentDto>();

            // 4. MỞ RỘNG NGỮ CẢNH (Context Expansion)
            foreach (var hit in anchorResponse.Hits)
            {
                var anchor = hit.Source;
                var anchorTime = anchor.CreatedAtUtc.UtcDateTime;

                // Tìm các tin nhắn trong vòng +/- 5 phút xung quanh tin nhắn mỏ neo
                var contextResponse = await _client.SearchAsync<MessageDoc>(s => s
                    .Index("chat_messages")
                    .Size(30)
                    .Query(q => q
                        .Bool(b => b.Must(
                            m => m.Term(t => t.Field(f => f.ConversationId).Value(anchor.ConversationId)),
                            m => m.DateRange(r => r
                                .Field(f => f.CreatedAtUtc)
                                .GreaterThanOrEquals(anchorTime.AddMinutes(-5))
                                .LessThanOrEquals(anchorTime.AddMinutes(5))
                            )
                        ))
                    )
                    .Sort(srt => srt.Ascending(f => f.CreatedAtUtc))
                );

                segments.Add(new ContextSegmentDto(
                    ConversationId: anchor.ConversationId,
                    RelevanceScore: hit.Score ?? 0,
                    AnchorMessage: anchor,
                    SurroundingMessages: contextResponse.Documents.ToList()
                ));
            }

            return Ok(segments);
        }
        [HttpGet("summarize-unread/{conversationId}")]
        public async Task<IActionResult> SummarizeUnread(string conversationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();


            var marker = await _context.UserReadMarkers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ConversationId == conversationId);

            var lastReadUtc = marker?.LastReadUtc ?? DateTimeOffset.UtcNow.AddDays(-1);

            var searchResponse = await _client.SearchAsync<MessageDoc>(s => s
                .Index("chat_messages")
                .Query(q => q
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t.Field(f => f.ConversationId).Value(conversationId)),
                            m => m.DateRange(r => r
                                .Field(f => f.CreatedAtUtc)
                                .GreaterThan(lastReadUtc.UtcDateTime)
                            )
                        )
                    )
                )
                .Sort(srt => srt.Ascending(f => f.CreatedAtUtc))
                .Size(100)
            );

            if (!searchResponse.Documents.Any())
            {
                return Ok(new { summary = "Bạn đã cập nhật tất cả tin nhắn mới.", count = 0 });
            }


            var chatHistory = string.Join("\n", searchResponse.Documents
                .Select(d => $"{d.SenderId}: {d.Content}"));

            try
            {
                var summary = await _embeddingService.SummarizeChatAsync(chatHistory);

                return Ok(new
                {
                    summary,
                    unreadCount = searchResponse.Documents.Count,
                    lastReadAt = lastReadUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Gemini tóm tắt cho hội thoại {ConversationId}", conversationId);
                return StatusCode(500, "Không thể tạo bản tóm tắt từ AI lúc này.");
            }
        }
        [AllowAnonymous]
        [HttpPost("seed-ad-categories")]
        public async Task<IActionResult> SeedAdCategories()
        {
            // 1. XÓA INDEX CŨ (Vì index cũ đang bị sai kiểu dữ liệu)
            var existsResponse = await _client.Indices.ExistsAsync("ad_categories");
            if (existsResponse.Exists)
            {
                await _client.Indices.DeleteAsync("ad_categories");
            }

            // 2. TẠO INDEX MỚI VỚI MAPPING CHUẨN CHO VECTOR
            await _client.Indices.CreateAsync("ad_categories", c => c
                .Map<AdCategoryDoc>(m => m
                    .Properties(p => p
                        .Keyword(k => k.Name(n => n.Id))
                        .Text(t => t.Name(n => n.CategoryName))
                        .DenseVector(dv => dv.Name(n => n.Embedding).Dimensions(768)) // Gemini Embedding có 768 chiều
                    )
                )
            );

            var categories = new[] {
                "Thuốc xương khớp", "Khám nhi", "Mẹ và bé", "Dược phẩm", "Thể thao",
                "Đồ công nghệ", "Bảo hiểm y tế", "Mỹ phẩm", "Du lịch", "Bất động sản",
                "Ô tô - Xe máy", "Thực phẩm chức năng", "Thời trang nam nữ", "Chăm sóc thú cưng",
                "Khóa học - Giáo dục", "Nha khoa thẩm mỹ", "Thiết bị gia dụng",
                "Đồng hồ - Trang sức", "Sách - Văn phòng phẩm", "Dụng cụ thể hình"
            };

            int count = 0;
            foreach (var cat in categories)
            {
                var vector = await _embeddingService.GetEmbeddingAsync(cat);
                var doc = new AdCategoryDoc { Id = Guid.NewGuid().ToString(), CategoryName = cat, Embedding = vector };

                var response = await _client.IndexAsync(doc, i => i.Index("ad_categories"));
                if (response.IsValid) count++;
            }

            return Ok($"Đã khởi tạo thành công {count} danh mục quảng cáo chuẩn Vector!");
        }

        [AllowAnonymous]
        [HttpGet("match-category")]
        public async Task<IActionResult> MatchCategory([FromQuery] string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Ok(string.Empty);

            var queryVector = await _embeddingService.GetEmbeddingAsync(text);

            var response = await _client.SearchAsync<AdCategoryDoc>(s => s
                .Index("ad_categories")
                .Query(q => q
                    .ScriptScore(ss => ss
                        .Query(qq => qq.Exists(e => e.Field(f => f.Embedding)))
                        .Script(sc => sc
                            .Source("cosineSimilarity(params.qv, 'embedding') + 1.0")
                            .Params(p => p.Add("qv", queryVector))
                        )
                    )
                )
                .Size(1)
            );

            // 3. THÊM ĐOẠN NÀY ĐỂ NẾU LỖI NÓ SẼ BÁO ĐỎ LÊN POSTMAN, KHÔNG IM LẶNG NỮA
            if (!response.IsValid)
            {
                _logger.LogError("Lỗi ES: {Reason}", response.ServerError?.Error?.Reason);
                return BadRequest($"Elasticsearch Error: {response.ServerError?.Error?.Reason}");
            }

            var bestMatch = response.Documents.FirstOrDefault();
            if (bestMatch != null && !string.IsNullOrEmpty(bestMatch.CategoryName))
            {
                return Content(bestMatch.CategoryName, "text/plain");
            }

            return Ok(string.Empty);
        }
        // API TỰ ĐỘNG CHẤM ĐIỂM AI TỪ FILE CSV
        [AllowAnonymous]
        [HttpPost("evaluate-accuracy")]
        public async Task<IActionResult> EvaluateAccuracy(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng upload file test_data.csv");

            var results = new List<object>();
            int correctCount = 0;
            int totalCount = 0;

            using var reader = new StreamReader(file.OpenReadStream());
            var header = await reader.ReadLineAsync(); // Bỏ qua dòng tiêu đề (CauChat,DanhMuc)

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Dọn dẹp dấu ngoặc kép ở đầu và cuối chuỗi do CSV sinh ra
                if (line.StartsWith("\"") && line.EndsWith("\""))
                {
                    line = line.Substring(1, line.Length - 2);
                }

                // Cắt chuỗi dựa trên ký tự phân cách của CSV
                var parts = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                if (parts.Length != 2) parts = line.Split(','); // Fallback nếu file lưu không có ngoặc kép
                if (parts.Length != 2) continue;

                var cauChat = parts[0].Trim('"');
                var expectedCategories = parts[1].Trim('"');

                // --- 1. GỌI AI GEMINI & ELASTICSEARCH ĐỂ DỰ ĐOÁN ---
                var queryVector = await _embeddingService.GetEmbeddingAsync(cauChat);
                var response = await _client.SearchAsync<AdCategoryDoc>(s => s
                    .Index("ad_categories")
                    .Query(q => q
                        .ScriptScore(ss => ss
                            .Query(qq => qq.Exists(e => e.Field(f => f.Embedding)))
                            .Script(sc => sc
                                .Source("cosineSimilarity(params.qv, 'embedding') + 1.0")
                                .Params(p => p.Add("qv", queryVector))
                            )
                        )
                    )
                    .Size(1) // Chỉ lấy 1 danh mục có điểm cao nhất
                );

                var bestMatch = response.Documents.FirstOrDefault();
                string aiResult = bestMatch?.CategoryName ?? "Không xác định";

                // --- 2. ĐỐI CHIẾU KẾT QUẢ ---
                // Nếu danh mục AI đoán CÓ NẰM TRONG chuỗi kỳ vọng của file CSV -> Tính là ĐÚNG
                bool isCorrect = expectedCategories.Contains(aiResult, StringComparison.OrdinalIgnoreCase);

                if (isCorrect) correctCount++;
                totalCount++;

                // Lưu lại lịch sử để in ra báo cáo
                results.Add(new
                {
                    CauChat = cauChat,
                    KyVong = expectedCategories,
                    AIDuDoan = aiResult,
                    KetQua = isCorrect ? "ĐÚNG" : "SAI"
                });
            }

            // --- 3. TÍNH TỔNG ĐIỂM CHÍNH XÁC (PRECISION) ---
            double accuracy = totalCount > 0 ? Math.Round((double)correctCount / totalCount * 100, 2) : 0;

            return Ok(new
            {
                ThongKe = new
                {
                    TongSoCauTest = totalCount,
                    SoCauDoanDung = correctCount,
                    TiLeChinhXac = $"{accuracy}%"
                },
                ChiTiet = results
            });
        }
        private string NormalizedQuery(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"ko", "không"}, {"k", "không"}, {"đc", "được"}, {"dc", "được"},
                {"bn", "bệnh nhân"}, {"bs", "bác sĩ"}, {"xn", "xét nghiệm"},
                {"ph", "phác đồ"}, {"cls", "cận lâm sàng"}
            };
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                var cleanWord = words[i].Trim().ToLower();
                if (dictionary.TryGetValue(cleanWord, out var normalWord))
                {
                    words[i] = normalWord;
                }
            }
            return string.Join(' ', words);
        }
    }
    
}

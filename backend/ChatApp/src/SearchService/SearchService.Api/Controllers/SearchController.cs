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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using SearchService.Api.DbContexts;
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
        public SearchController(IElasticClient client, IEmbeddingService embeddingService, SearchDbContext searchDbContext,ILogger<SearchController> logger)
        {
            _embeddingService = embeddingService;
            _client = client;
            _logger = logger;
            _context = searchDbContext;
        }
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] string? conversationId)
        {
            if (string.IsNullOrWhiteSpace(term)) return BadRequest();

            var response = await _client.SearchAsync<MessageDoc>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .Match(mt => mt
                                .Field(f => f.Content)
                                .Query(term)
                                .Fuzziness(Fuzziness.Auto) 
                            )
                        )
                        .Filter(f =>
                        {
                            if (string.IsNullOrEmpty(conversationId))
                                return f.MatchAll();

                            return f.Term(t => t
                                .Field("conversationId.keyword") 
                                .Value(conversationId)          
                            );
                        })
                    )
                )
                .Size(20) 
            );

            return Ok(response.Documents);
        }
        [HttpGet("hybrid")]
        public async Task<IActionResult> HybridSearch([FromQuery] string term, [FromQuery] string? conversationId)
        {
            if (string.IsNullOrWhiteSpace(term)) return BadRequest();

            var queryVector = await _embeddingService.GetEmbeddingAsync(term);
            if (queryVector.Length == 0) return Ok(Array.Empty<MessageDoc>());
            if (queryVector.Length != 768)
                return StatusCode(500, $"Embedding dim mismatch: {queryVector.Length} (expected 768)");
            var response = await _client.SearchAsync<MessageDoc>(s => s
                .Index("chat_messages")
                .Size(20)
                .Query(q => q
                    .ScriptScore(ss => ss
                        .Query(qq => qq
                            .Bool(b => b
                                // Filter Conversation
                                .Filter(f => !string.IsNullOrEmpty(conversationId)
                                    ? f.Term(t => t.Field("conversationId.keyword").Value(conversationId))
                                    : f.MatchAll()
                                )
                                .Must(m => m.Exists(e => e.Field(f => f.Embedding)))
                            )
                        )
                        .Script(sc => sc
                            .Source(@"
                                double v = cosineSimilarity(params.qv, 'embedding') + 1.0;
                                return v; 
                            ") 
                            .Params(p => p.Add("qv", queryVector))
                        )
                    )
                )
            );

            if (!response.IsValid)
                return StatusCode(500, response.ServerError?.ToString() ?? response.OriginalException?.Message);

            return Ok(response.Documents);
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
    }
}

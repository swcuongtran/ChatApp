using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nest;
using SearchService.Api.Model;
using SearchService.Api.Services;

namespace SearchService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly IElasticClient _client;
        private readonly IEmbeddingService _embeddingService;
        public SearchController(IElasticClient client, IEmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;
            _client = client;
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
                                // BẮT BUỘC CÓ DÒNG NÀY: Chỉ tính điểm cho docs có cột Embedding
                                .Must(m => m.Exists(e => e.Field(f => f.Embedding)))
                            )
                        )
                        .Script(sc => sc
                            .Source(@"
                                double v = cosineSimilarity(params.qv, 'embedding') + 1.0;
                                return v; 
                            ") // Tạm bỏ _score + keyword match để test riêng sức mạnh Vector AI trước cho dễ hiểu
                            .Params(p => p.Add("qv", queryVector))
                        )
                    )
                )
            );

            if (!response.IsValid)
                return StatusCode(500, response.ServerError?.ToString() ?? response.OriginalException?.Message);

            return Ok(response.Documents);
        }
    }
}

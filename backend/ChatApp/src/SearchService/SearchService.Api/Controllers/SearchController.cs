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

            // 1. Tạo vector từ câu tìm kiếm của User
            var queryVector = await _embeddingService.GetEmbeddingAsync(term);

            // 2. Query Elasticsearch (Kết hợp Match + Vector)
            var response = await _client.SearchAsync<MessageDoc>(s => s
                .Size(20)
                .Query(q => q
                    .Bool(b => b
                        .Filter(f => !string.IsNullOrEmpty(conversationId)
                            ? f.Term(t => t.ConversationId, conversationId)
                            : f.MatchAll()
                        )
                        .Should(
                            // A. Tìm từ khóa chính xác (Ưu tiên 1.5)
                            sh => sh.Match(m => m
                                .Field(f => f.Content)
                                .Query(term)
                                .Boost(1.5)
                            ),
                            // B. Tìm ngữ nghĩa (Ưu tiên 1.0)
                            sh => sh.ScriptScore(ss => ss
                                .Query(sq => sq.MatchAll())
                                .Script(sc => sc
                                    .Source("cosineSimilarity(params.query_vector, 'embedding') + 1.0")
                                    .Params(p => p.Add("query_vector", queryVector))
                                )
                                .Boost(1.0)
                            )
                        )
                        .MinimumShouldMatch(1)
                    )
                )
            );

            return Ok(response.Documents);
        }
    }
}

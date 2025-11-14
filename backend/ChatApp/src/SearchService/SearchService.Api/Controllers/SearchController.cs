using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nest;
using SearchService.Api.Model;

namespace SearchService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly IElasticClient _client;
        public SearchController(IElasticClient client)
        {
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
    }
}

using AnalyticsService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace AnalyticsService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly ISender _sender;

        public StatsController(ISender sender) => _sender = sender;

        [HttpGet("daily")]
        public async Task<IActionResult> GetDailyStats([FromQuery] int days = 30)
        {
            var query = new GetDailyStatsQuery(days);

            var result = await _sender.Send(query);

            return Ok(result);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetTotalSummary()
        {
            var query = new GetSummaryQuery();

            var result = await _sender.Send(query);

            return Ok(result);
        }
        [HttpGet("user/daily")]
        public async Task<IActionResult> GetUserDailyStats([FromQuery] int days = 7)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var query = new GetUserDailyStatsQuery(userId, days);
            var result = await _sender.Send(query);

            return Ok(result);
        }
    }
}
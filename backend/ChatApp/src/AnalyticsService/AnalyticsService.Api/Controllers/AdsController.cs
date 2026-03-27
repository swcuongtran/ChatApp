using AnalyticsService.Infrastructure.MongoDb;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace AnalyticsService.Api.Controllers
{
    [Route("api/ads")]
    [ApiController]
    public class AdsController : ControllerBase
    {
        private readonly IMongoDbContext _dbContext;

        public AdsController(IMongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetAdRecommendations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var today = DateTime.UtcNow.Date;

            // 1. Kéo giỏ hàng hôm nay của User lên
            var currentBasket = await _dbContext.UserBaskets
                .Find(b => b.UserId == userId && b.Date == today)
                .FirstOrDefaultAsync();

            // Nếu user chưa chat gì hôm nay -> Trả về quảng cáo mặc định (Cold-start)
            if (currentBasket == null || !currentBasket.Categories.Any())
            {
                return Ok(new { category = "Quảng cáo mặc định (Ví dụ: Giảm giá 50%)" });
            }

            // 2. Kéo các luật từ Apriori lên (Đã sắp xếp theo Confidence giảm dần)
            var rules = await _dbContext.AdRules
                .Find(_ => true)
                .SortByDescending(r => r.Confidence)
                .ToListAsync();

            // 3. Tìm luật khớp nhất: Luật mà TẤT CẢ điều kiện (Antecedents) đều nằm trong giỏ hàng của User
            var matchedRule = rules.FirstOrDefault(r =>
                r.Antecedents.All(a => currentBasket.Categories.Contains(a)));

            if (matchedRule != null)
            {
                return Ok(new { category = matchedRule.Consequent }); // Trả về kết quả suy ra từ luật
            }

            // Fallback: Nếu không có luật nào khớp, lấy luôn category gần nhất họ vừa nói đến
            return Ok(new { category = currentBasket.Categories.First() });
        }
    }
}

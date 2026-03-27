using AnalyticsService.Application.Services;
using AnalyticsService.Infrastructure.MongoDb;
using AnalyticsService.Infrastructure.MongoDb.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AnalyticsService.Infrastructure.Workers
{
    public class AprioriWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AprioriWorker> _logger;

        public AprioriWorker(IServiceProvider serviceProvider, ILogger<AprioriWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AprioriWorker started. Chờ lịch chạy...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Đang setup chạy mỗi 10 phút 1 lần để bạn dễ test khi chấm đồ án
                    // Nếu muốn chạy ban đêm thì dùng logic: if (DateTime.Now.Hour == 2)
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                    _logger.LogInformation("[Data Mining] Bắt đầu chạy Apriori...");

                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<IMongoDbContext>();
                    var aprioriService = scope.ServiceProvider.GetRequiredService<AprioriService>();

                    // 1. Kéo toàn bộ giỏ hàng có từ 2 Category trở lên
                    var baskets = await dbContext.UserBaskets.Find(b => b.Categories.Count >= 2).ToListAsync(stoppingToken);
                    var transactions = baskets.Select(b => new HashSet<string>(b.Categories)).ToList();

                    if (transactions.Count > 0)
                    {
                        // 2. Ném vào máy xay Apriori (Support: 5%, Confidence: 50%)
                        var rules = aprioriService.MineRules(transactions, minSupport: 0.05, minConfidence: 0.50);

                        if (rules.Any())
                        {
                            // 3. Xóa bộ luật cũ và lưu bộ luật mới
                            await dbContext.AdRules.DeleteManyAsync(_ => true, stoppingToken);

                            var ruleDocs = rules.Select(r => new AdRuleDocument
                            {
                                Antecedents = r.Antecedents,
                                Consequent = r.Consequent,
                                Confidence = r.Confidence,
                                Support = r.Support
                            }).ToList();

                            await dbContext.AdRules.InsertManyAsync(ruleDocs, cancellationToken: stoppingToken);

                            _logger.LogInformation("[Data Mining] Sinh thành công {Count} luật liên kết!", rules.Count);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi đào dữ liệu bằng AprioriWorker.");
                }
            }
        }
    }
}
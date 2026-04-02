using AnalyticsService.Application.Abstractions;
using AnalyticsService.Domain.Entities;
using BuildingBlock.Messaging;
using Confluent.Kafka;
using Contracts;
using Contracts.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AnalyticsService.Infrastructure.Workers
{
    public class AdBasketConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AdBasketConsumer> _logger;
        private readonly string _kafkaBootstrapServers;

        public AdBasketConsumer(IServiceProvider serviceProvider, ILogger<AdBasketConsumer> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _kafkaBootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:9092";
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var conf = new ConsumerConfig
            {
                BootstrapServers = _kafkaBootstrapServers,
                GroupId = "ad-basket-group", // TÊN GROUP PHẢI KHÁC STATS CONSUMER
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(conf).Build();
            consumer.Subscribe(Topics.ChatMessageCreated); // Chỉ hóng đúng topic chat

            _logger.LogInformation("AdBasketConsumer started.");
            await Task.Yield();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    var envelope = JsonSerializer.Deserialize<IntegrationEvent<ChatMessageCreatedV1>>(cr.Message.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (envelope?.Data is not null && !string.IsNullOrWhiteSpace(envelope.Data.Content))
                    {
                        var msg = envelope.Data;

                        using var scope = _serviceProvider.CreateScope();
                        var basketRepo = scope.ServiceProvider.GetRequiredService<IBasketRepository>();
                        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                        // 1. Gọi SearchService phân tích Vector
                        var client = httpClientFactory.CreateClient();
                        var searchUrl = $"http://searchservice.api:8080/api/search/match-category?text={Uri.EscapeDataString(msg.Content)}";
                        var response = await client.GetAsync(searchUrl, stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            var matchedCategory = await response.Content.ReadAsStringAsync();
                            matchedCategory = matchedCategory.Trim().Replace("\"", "");

                            if (!string.IsNullOrWhiteSpace(matchedCategory))
                            {
                                var today = DateTime.UtcNow.Date;

                                // 2. Lấy giỏ hàng & Cập nhật
                                var basket = await basketRepo.GetBasketAsync(msg.SenderId, today, stoppingToken);
                                if (basket == null) basket = new UserBasket(msg.SenderId, today);

                                basket.AddCategory(matchedCategory);
                                await basketRepo.UpsertBasketAsync(basket, stoppingToken);

                                _logger.LogInformation("Added category '{Cat}' for user {UserId}", matchedCategory, msg.SenderId);
                            }
                        }
                    }

                    consumer.Commit(cr);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi ở AdBasketConsumer. Đợi 2s...");
                    await Task.Delay(2000, stoppingToken);
                }
            }
            consumer.Close();
        }
    }
}


using BuildingBlock.Messaging;
using Confluent.Kafka;
using Contracts;
using Contracts.Chat;
using Microsoft.EntityFrameworkCore;
using SearchService.Api.DbContexts;
using SearchService.Api.Model;
using System.Text.Json;

namespace SearchService.Api.Workers
{
    public class UserReadConsumer : BackgroundService
    {
        private readonly ILogger<UserReadConsumer> _logger;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserReadConsumer(
            ILogger<UserReadConsumer> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _config = configuration;
            _scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var conf = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = "search-service-read-marker-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
            using var consumer = new ConsumerBuilder<string, string>(conf).Build();
            consumer.Subscribe(Topics.UserReadMessage);
            _logger.LogInformation("UserReadConsumer started and subscribed to topic {Topic}", Topics.UserReadMessage);
            await Task.Yield();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    var json = cr.Message.Value;

                    var envelope = JsonSerializer.Deserialize<IntegrationEvent<UserReadMessageV1>>(json);

                    if (envelope.Data != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
                        var data = envelope.Data;
                        var marker = await dbContext.UserReadMarkers.FirstOrDefaultAsync(x => x.UserId == data.UserId && x.ConversationId == data.ConversationId, stoppingToken);
                        if (marker == null)
                        {
                            dbContext.UserReadMarkers.Add(new UserReadMarker
                            {
                                UserId = data.UserId,
                                ConversationId = data.ConversationId,
                                LastReadUtc = data.ReadAtUtc
                            });
                        }
                        else if (data.ReadAtUtc > marker.LastReadUtc)
                        {
                            marker.LastReadUtc = data.ReadAtUtc;
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                        consumer.Commit(cr);
                        _logger.LogDebug("Processed UserReadMessage for UserId: {UserId}, ConversationId: {ConversationId}, ReadAtUtc: {ReadAtUtc}", data.UserId, data.ConversationId, data.ReadAtUtc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from topic {Topic}", Topics.UserReadMessage);
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}

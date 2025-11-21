
using AnalyticsService.Infrastructure.MongoDb;
using AnalyticsService.Infrastructure.MongoDb.Documents;
using BuildingBlock.Messaging;
using Confluent.Kafka;
using Contracts;
using Contracts.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Text.Json;

namespace AnalyticsService.Api.Workers
{
    public class StatsConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StatsConsumer> _logger;
        private readonly string _kafkaBootstrapServers;
        public StatsConsumer(IServiceProvider serviceProvider, ILogger<StatsConsumer> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _kafkaBootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "kafka:9092";
        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var conf = new ConsumerConfig
            {
                BootstrapServers = _kafkaBootstrapServers,
                GroupId = "stats-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
            using var consumer = new ConsumerBuilder<string, string>(conf).Build();
            consumer.Subscribe(new[] { Topics.ChatMessageCreated, Topics.AttachmentUploaded, Topics.ConversationCreated });

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<IMongoDbContext>();

                    var today = DateTime.UtcNow.Date;
                    var todayId = today.ToString("yyyy-MM-dd");
                    var collection = dbContext.DailyStats;

                    var filter = Builders<DailyStatDocument>.Filter.Eq(s => s.Id, todayId);
                    UpdateDefinition<DailyStatDocument> update;

                    if (cr.Topic == Topics.ChatMessageCreated)
                    {
                        update = Builders<DailyStatDocument>.Update.Inc(s => s.TotalMessages, 1)
                            .SetOnInsert(s => s.Date, today);
                    }
                    else if (cr.Topic == Topics.AttachmentUploaded)
                    {
                        var envelope = JsonSerializer.Deserialize<IntegrationEvent<AttachmentUploadedV1>>(cr.Message.Value)!;
                        var fileSize = envelope.Data.SizeInBytes;

                        update = Builders<DailyStatDocument>.Update
                            .Inc(s => s.TotalFiles, 1)
                            .Inc(s => s.TotalStorageBytes, fileSize)
                            .SetOnInsert(s => s.Date, today);
                    }
                    else if (cr.Topic == Topics.ConversationCreated)
                    {
                        update = Builders<DailyStatDocument>.Update.Inc(s => s.NewConversations, 1)
                            .SetOnInsert(s => s.Date, today);
                    }
                    else continue;

                    await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, stoppingToken);
                    consumer.Commit(cr);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Kafka message in StatsConsumer");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}

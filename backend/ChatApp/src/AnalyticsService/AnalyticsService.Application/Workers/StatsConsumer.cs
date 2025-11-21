using AnalyticsService.Infrastructure.MongoDb;
using AnalyticsService.Infrastructure.MongoDb.Documents;
using BuildingBlock.Messaging;
using Confluent.Kafka;
using Contracts;
using Contracts.Chat;
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
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

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

            _logger.LogInformation("StatsConsumer subscribed and running.");

            try 
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = consumer.Consume(stoppingToken);

                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<IMongoDbContext>();

                        var today = DateTime.UtcNow.Date;
                        var dateId = today.ToString("yyyy-MM-dd");

                        var globalCollection = dbContext.DailyStats;
                        var userStatCollection = dbContext.UserDailyStats;

                        var globalFilter = Builders<DailyStatDocument>.Filter.Eq(s => s.Id, dateId);
                        UpdateDefinition<DailyStatDocument> globalUpdate = null!;

                        if (cr.Topic == Topics.ChatMessageCreated)
                        {
                            var envelope = JsonSerializer.Deserialize<IntegrationEvent<ChatMessageCreatedV1>>(cr.Message.Value, JsonOpts);

                            if (envelope?.Data is not null)
                            {
                                string senderId = envelope.Data.SenderId;

                                globalUpdate = Builders<DailyStatDocument>.Update.Inc(s => s.TotalMessages, 1)
                                    .SetOnInsert(s => s.Date, today);

                                var userStatId = $"{senderId}_{dateId}";
                                var userFilter = Builders<DailyUserStatDocument>.Filter.Eq(d => d.Id, userStatId);
                                var userUpdate = Builders<DailyUserStatDocument>.Update
                                    .Inc(s => s.MessagesSent, 1)
                                    .SetOnInsert(s => s.Date, today)
                                    .SetOnInsert(s => s.UserId, senderId);

                                await userStatCollection.UpdateOneAsync(userFilter, userUpdate, new UpdateOptions { IsUpsert = true }, stoppingToken);
                            }
                        }
                        else if (cr.Topic == Topics.AttachmentUploaded)
                        {
                            var envelope = JsonSerializer.Deserialize<IntegrationEvent<AttachmentUploadedV1>>(cr.Message.Value, JsonOpts);

                            if (envelope?.Data is not null)
                            {
                                string senderId = envelope.Data.UploadedByUserId;
                                var fileSize = envelope.Data.SizeInBytes;

                                globalUpdate = Builders<DailyStatDocument>.Update
                                    .Inc(s => s.TotalFiles, 1)
                                    .Inc(s => s.TotalStorageBytes, fileSize)
                                    .SetOnInsert(s => s.Date, today);

                                var userStatId = $"{senderId}_{dateId}";
                                var userFilter = Builders<DailyUserStatDocument>.Filter.Eq(d => d.Id, userStatId);
                                var userUpdate = Builders<DailyUserStatDocument>.Update
                                    .Inc(s => s.FilesUploaded, 1)
                                    .Inc(s => s.StorageUsedBytes, fileSize)
                                    .SetOnInsert(s => s.Date, today)
                                    .SetOnInsert(s => s.UserId, senderId);

                                await userStatCollection.UpdateOneAsync(userFilter, userUpdate, new UpdateOptions { IsUpsert = true }, stoppingToken);
                            }
                        }
                        else if (cr.Topic == Topics.ConversationCreated)
                        {
                            globalUpdate = Builders<DailyStatDocument>.Update.Inc(s => s.NewConversations, 1)
                                .SetOnInsert(s => s.Date, today);
                        }
                        else
                        {
                            consumer.Commit(cr);
                            continue;
                        }

                        if (globalUpdate != null)
                        {
                            await globalCollection.UpdateOneAsync(globalFilter, globalUpdate, new UpdateOptions { IsUpsert = true }, stoppingToken);
                        }

                        consumer.Commit(cr);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {

                        _logger.LogError(ex, "Error processing Kafka message in StatsConsumer. Retrying...");
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("StatsConsumer worker is shutting down gracefully.");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}
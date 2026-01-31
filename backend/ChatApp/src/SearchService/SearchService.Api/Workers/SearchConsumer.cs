
using BuildingBlock.Messaging;
using Confluent.Kafka;
using Contracts;
using Contracts.Chat;
using Nest;
using SearchService.Api.Model;
using SearchService.Api.Services;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SearchService.Api.Workers
{
    public class SearchConsumer : BackgroundService
    {
        private readonly ILogger<SearchConsumer> _logger;
        private readonly IConfiguration _config;
        private readonly IElasticClient _client;
        private readonly ActivitySource _activitySource = new ActivitySource("SearchService");
        private readonly IEmbeddingService _embeddingService;
        public SearchConsumer(
            ILogger<SearchConsumer> logger,
            IConfiguration configuration,
            IElasticClient elasticClient,
            IEmbeddingService embeddingService)
        {
            _logger = logger;
            _config = configuration;
            _client = elasticClient;
            _embeddingService = embeddingService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var conf = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = _config["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(conf).Build();
            consumer.Subscribe(Topics.ChatMessageCreated);

            _logger.LogInformation("SearchConsumer started, listening to topic: {Topic}", Topics.ChatMessageCreated);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);

                    ActivityContext parentContext = default;
                    if (cr.Message.Headers.TryGetLastBytes("x-trace-id", out var traceIdBytes))
                    {
                        var traceIdStr = Encoding.UTF8.GetString(traceIdBytes);
                        try
                        {
                            var traceId = ActivityTraceId.CreateFromString(traceIdStr.AsSpan());
                            parentContext = new ActivityContext(traceId, ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
                        }
                        catch { }
                    }
                    using var activity = _activitySource.StartActivity("IndexMessageToElastic", ActivityKind.Consumer, parentContext);

                    var json = cr.Message.Value;
                    var envelope = JsonSerializer.Deserialize<IntegrationEvent<ChatMessageCreatedV1>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (envelope?.Data is not null)
                    {
                        var msg = envelope.Data;
                        float[]? vector = null;
                        try
                        {
                            vector = await _embeddingService.GetEmbeddingAsync(msg.Content);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate embedding for message {MessageId}", msg.MessageId);
                        }
                        var doc = new MessageDoc
                        {
                            Id = msg.MessageId,
                            ConversationId = msg.ConversationId,
                            SenderId = msg.SenderId,
                            Content = msg.Content,
                            CreatedAtUtc = msg.CreatedAtUtc,
                            Embedding = vector
                        };

                        var response = await _client.IndexDocumentAsync(doc, stoppingToken);
                        if (!response.IsValid)
                        {
                            _logger.LogError("Failed to index message {MessageId}: {Error}", msg.MessageId, response.OriginalException?.Message);
                        }
                        else
                        {
                            _logger.LogInformation("Indexed message {MessageId} into Elasticsearch", msg.MessageId);
                            consumer.Commit(cr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SearchConsumer");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}

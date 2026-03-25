
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
            await Task.Yield();
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
                        int retryCount = 0;

                        // VÒNG LẶP RETRY: Cầm chân tiến trình cho đến khi lấy được Vector
                        while (vector == null && !stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                // Tránh trường hợp text rỗng gây lỗi
                                if (string.IsNullOrWhiteSpace(msg.Content))
                                {
                                    vector = Array.Empty<float>();
                                    break;
                                }

                                vector = await _embeddingService.GetEmbeddingAsync(msg.Content);
                            }
                            catch (Exception ex)
                            {
                                retryCount++;
                                // Thời gian đợi tăng dần: 2s, 4s, 6s... tối đa 30s để Gemini kịp "thở"
                                int delaySeconds = Math.Min(retryCount * 2, 30);
                                _logger.LogWarning(ex, "[Rate Limit] Lỗi gọi Gemini cho tin nhắn {MessageId}. Thử lại lần {RetryCount}. Đợi {Delay}s...", msg.MessageId, retryCount, delaySeconds);

                                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                            }
                        }

                        // Nếu vòng lặp dừng vì app bị tắt (Cancel) thì không làm tiếp
                        if (vector == null) continue;

                        var doc = new MessageDoc
                        {
                            Id = msg.MessageId,
                            ConversationId = msg.ConversationId,
                            SenderId = msg.SenderId,
                            Content = msg.Content,
                            CreatedAtUtc = msg.CreatedAtUtc,
                            Embedding = vector.Length > 0 ? vector : null // Gán null nếu text rỗng không có vector
                        };

                        var response = await _client.IndexDocumentAsync(doc, stoppingToken);
                        if (!response.IsValid)
                        {
                            // Nếu lưu vào Elasticsearch lỗi, NÉM EXCEPTION để văng ra vòng Catch bên ngoài
                            // Kafka sẽ không commit, và tin nhắn sẽ được fetch lại.
                            throw new Exception($"Lỗi lưu Elasticsearch cho tin {msg.MessageId}: {response.OriginalException?.Message}");
                        }

                        _logger.LogInformation("Indexed message {MessageId} into Elasticsearch successfully", msg.MessageId);

                        // LỆNH QUAN TRỌNG NHẤT: CHỈ COMMIT KHI ĐÃ CÓ VECTOR VÀ LƯU ES THÀNH CÔNG
                        consumer.Commit(cr);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SearchConsumer processing loop. Will try processing next message or re-fetch.");
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }
    }
}

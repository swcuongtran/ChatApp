using BuildingBlock.Messaging;
using Confluent.Kafka;
using Contracts;
using Contracts.Chat;
using DeliveryService.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DeliveryService.Api.Workers
{
    public class KafkaMessageConsumer : BackgroundService
    {
        private readonly ILogger<KafkaMessageConsumer> _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly IHubContext<ChatHub> _hubContext;
        private static readonly ActivitySource _activitySource = new ActivitySource("DeliveryService");

        public KafkaMessageConsumer(ILogger<KafkaMessageConsumer> logger, IConfiguration configuration, IHubContext<ChatHub> hubContext)
        {
            _logger = logger;
            _kafkaOptions = configuration.GetSection(KafkaOptions.Kafka).Get<KafkaOptions>() ??
                throw new InvalidOperationException("Kafka configuration is missing.");
            _hubContext = hubContext;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            _logger.LogInformation("Kafka Message Consumer running.");

            try
            {
                await StartConsumer(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error in Kafka Consumer. Background worker stopped.");
            }
        }

        private async Task StartConsumer(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaOptions.BootstrapServers,
                GroupId = _kafkaOptions.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // VÒNG LẶP 1: Đảm bảo Consumer luôn được tạo lại nếu bị sập
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Tạo Consumer mới mỗi khi vòng lặp này bắt đầu lại
                    using var consumer = new ConsumerBuilder<string, string>(config)
                        .SetErrorHandler((_, e) => _logger.LogError("Kafka Error: {Reason}", e.Reason))
                        // .SetLogHandler((_, log) => _logger.LogDebug("Kafka Log: {Message}", log.Message)) // Bớt log rác nếu cần
                        .Build();

                    consumer.Subscribe(Topics.ChatMessageCreated);
                    _logger.LogInformation("Kafka Consumer (Re)Started and subscribed to {Topic}", Topics.ChatMessageCreated);

                    // VÒNG LẶP 2: Vòng lặp tiêu thụ tin nhắn
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(stoppingToken);

                            // --- BẮT ĐẦU XỬ LÝ TIN NHẮN ---
                            var headers = consumeResult.Message.Headers;
                            ActivityContext parentContext = default;

                            if (headers.TryGetLastBytes("x-trace-id", out var traceIdBytes))
                            {
                                var traceIdStr = Encoding.UTF8.GetString(traceIdBytes);
                                try
                                {
                                    var traceId = ActivityTraceId.CreateFromString(traceIdStr.AsSpan());
                                    parentContext = new ActivityContext(traceId, ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
                                }
                                catch { /* Ignore invalid trace format */ }
                            }

                            using var activity = _activitySource.StartActivity("ProcessKafkaMessage", ActivityKind.Consumer, parentContext);
                            var jsonPayload = consumeResult.Message.Value;

                            try
                            {
                                var envelope = JsonSerializer.Deserialize<IntegrationEvent<ChatMessageCreatedV1>>(jsonPayload, jsonSerializerOptions);

                                if (envelope == null || envelope.Data == null)
                                {
                                    _logger.LogError("Null data received: {Payload}", jsonPayload);
                                    consumer.Commit(consumeResult);
                                    continue;
                                }

                                var messageEvent = envelope.Data;

                                await _hubContext.Clients
                                    .Group(messageEvent.ConversationId)
                                    .SendAsync("ReceiveMessage", messageEvent, stoppingToken);

                                _logger.LogInformation("Dispatched msg {MsgId} to Group {ConvId}", messageEvent.MessageId, messageEvent.ConversationId);

                                consumer.Commit(consumeResult);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing logic: {Payload}", jsonPayload);
                            }
                            // --- KẾT THÚC XỬ LÝ TIN NHẮN ---
                        }
                        catch (ConsumeException ex)
                        {
                            if (ex.Error.IsFatal)
                            {
                                _logger.LogCritical(ex, "Fatal Kafka Error. Re-initializing consumer...");
                                // QUAN TRỌNG: Break khỏi vòng lặp bên trong để 'using' statement dispose consumer hiện tại
                                // và vòng lặp bên ngoài sẽ tạo consumer mới.
                                break;
                            }
                            else
                            {
                                _logger.LogWarning("Non-fatal Kafka Error: {Reason}", ex.Error.Reason);
                                await Task.Delay(1000, stoppingToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Bắt các lỗi ngoại lệ khác (ví dụ lỗi kết nối mạng khi tạo consumer) để Service không bao giờ crash
                    _logger.LogError(ex, "Unexpected error in Kafka Consumer loop. Retrying in 5s...");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}
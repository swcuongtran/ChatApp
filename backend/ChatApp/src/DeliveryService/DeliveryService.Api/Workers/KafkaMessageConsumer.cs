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

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kafka Message Consumer running.");
           
            return StartConsumer(stoppingToken);
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

            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka Error: {Reason}", e.Reason))
                .SetLogHandler((_, log) => _logger.LogDebug("Kafka Log: {Message}", log.Message))
                .Build();

            consumer.Subscribe(Topics.ChatMessageCreated);
            _logger.LogInformation("Kafka Consumer subscribed to topic {Topic}", Topics.ChatMessageCreated);

            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        var headers = consumeResult.Message.Headers;

                        ActivityContext parentContext = default; 

                       if (consumeResult.Message.Headers.TryGetLastBytes("x-trace-id", out var traceIdBytes))
                        {
                            var traceIdStr = Encoding.UTF8.GetString(traceIdBytes);
                            try
                            {
                                var traceId = ActivityTraceId.CreateFromString(traceIdStr.AsSpan());

                                parentContext = new ActivityContext(traceId, ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
                            }
                            catch
                            {
                                _logger.LogDebug("Invalid TraceId format from Kafka header: {TraceId}", traceIdStr);
                            }
                        }

                        using var activity = _activitySource.StartActivity("ProcessKafkaMessage", ActivityKind.Consumer, parentContext);

                        activity?.SetTag("messaging.system", "kafka");
                        activity?.SetTag("messaging.destination", Topics.ChatMessageCreated);
                        var jsonPayload = consumeResult.Message.Value;

                        try
                        {
                            var envelope = JsonSerializer.Deserialize<IntegrationEvent<ChatMessageCreatedV1>>(jsonPayload, jsonSerializerOptions);

                            if (envelope == null || envelope.Data == null)
                            {
                                _logger.LogError("Failed to deserialize Kafka message or Data is null: {Payload}", jsonPayload);
                                consumer.Commit(consumeResult); 
                                continue;
                            }

                            var messageEvent = envelope.Data;
                            var conversationId = messageEvent.ConversationId;

                            
                            await _hubContext.Clients
                                .Group(conversationId)
                                .SendAsync("ReceiveMessage", messageEvent, stoppingToken);

                            _logger.LogInformation("Dispatched message {MessageId} to group {ConvId}",
                                messageEvent.MessageId, conversationId);

                            
                            consumer.Commit(consumeResult);
                            activity?.SetTag("signalr.group", envelope.Data.ConversationId);
                            activity?.SetStatus(ActivityStatusCode.Ok);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing Kafka message logic: {Payload}", jsonPayload);
                            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        if (!ex.Error.IsFatal)
                        {
                            _logger.LogWarning("Kafka consumer non-fatal error (e.g. Topic not created yet): {Reason}. Retrying in 1s...", ex.Error.Reason);
                            await Task.Delay(1000, stoppingToken);
                        }
                        else
                        {
                            _logger.LogCritical(ex, "Fatal Kafka consumer error.");
                            throw; 
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka Consumer is stopping.");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}
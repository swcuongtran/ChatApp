using Confluent.Kafka;
using Contracts;
using Contracts.Chat;
using DeliveryService.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using BuildingBlock.Messaging; // Cần thêm namespace này

namespace DeliveryService.Api.Workers
{
    public class KafkaMessageConsumer : BackgroundService
    {
        private readonly ILogger<KafkaMessageConsumer> _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly IHubContext<ChatHub> _hubContext;

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
            return Task.Run(() => StartConsumer(stoppingToken), stoppingToken);
        }

        private void StartConsumer(CancellationToken stoppingToken)
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
                    var consumeResult = consumer.Consume(stoppingToken);
                    var jsonPayload = consumeResult.Message.Value;

                    try
                    {
                        
                        var envelope = JsonSerializer.Deserialize<IntegrationEvent<ChatMessageCreatedV1>>(jsonPayload, jsonSerializerOptions);

                        if (envelope == null || envelope.Data == null)
                        {
                            _logger.LogError("Failed to deserialize Kafka message or Data is null: {Payload}", jsonPayload);
                            continue;
                        }

                        
                        var messageEvent = envelope.Data;
                        var conversationId = messageEvent.ConversationId;

                        _hubContext.Clients
                            .Group(conversationId)
                            .SendAsync("ReceiveMessage", messageEvent, stoppingToken);

                        _logger.LogInformation("Dispatched message {MessageId} to group {ConvId}",
                            messageEvent.MessageId, conversationId);

                        consumer.Commit(consumeResult);
                    }
                    catch (Exception ex)
                    {
                        
                        _logger.LogError(ex, "Error processing Kafka message: {Payload}", jsonPayload);
                       
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
using BuildingBlock.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatService.Infrastructure.Messaging
{
    public sealed class KafkaOptions
    {
        public string Broker { get; set; } = "kafka:9092";
        public string ClientId { get; set; } = "ChatService";
    }

    public class KafkaEventBus : IEventBus, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaEventBus> _logger;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public KafkaEventBus(KafkaOptions options, ILogger<KafkaEventBus> logger)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = options.Broker,
                ClientId = options.ClientId,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 5,
                LingerMs = 5,
                CompressionType = CompressionType.Snappy
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger = logger;
        }
        public void Dispose()
        {
            _producer.Dispose();
        }

        public async Task PublishAsync(string topic, string key, IntegrationEvent message, CancellationToken cancellationToken = default)
        {
            var payload = JsonSerializer.Serialize(message, _jsonOpts);

            var headers = new Headers();
            if (!string.IsNullOrWhiteSpace(message.Header.TraceId))
                headers.Add("x-trace-id", System.Text.Encoding.UTF8.GetBytes(message.Header.TraceId));
            if (!string.IsNullOrWhiteSpace(message.Header.CorrelationId))
                headers.Add("x-correlation-id", System.Text.Encoding.UTF8.GetBytes(message.Header.CorrelationId));
            if (!string.IsNullOrWhiteSpace(message.Header.SchemaVersion))
                headers.Add("x-schema-version", System.Text.Encoding.UTF8.GetBytes(message.Header.SchemaVersion));
            if (!string.IsNullOrWhiteSpace(message.Header.Producer))
                headers.Add("x-producer", System.Text.Encoding.UTF8.GetBytes(message.Header.Producer));

            var kafkaMessage = new Message<string, string>
            {
                Key = string.IsNullOrWhiteSpace(key) ? message.EventId : key,
                Value = payload,
                Headers = headers
            };
            try
            {
                var dr = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

                _logger.LogInformation("Published event {EventId} to topic {Topic} at offset {Offset}", message.EventId, topic, dr.Offset);
            }
            catch (ProduceException<string, string> pex)
            {
                _logger.LogError(pex, "Produce exception for event {EventId} to topic {Topic}: {Reason}", message.EventId, topic, pex.Error.Reason);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event {EventId} to topic {Topic}", message.EventId, topic);
                throw;
            }
        }
    }
}

using BuildingBlock.Messaging;
using Confluent.Kafka;
using System.Text.Json;

namespace FileService.Api.Infrastructure
{
    public class KafkaEventBus : IEventBus, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaEventBus> _logger;

        public KafkaEventBus(IConfiguration configuration, ILogger<KafkaEventBus> logger)
        {
            _logger = logger;
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                ClientId = "FileService",
                Acks = Acks.All
            };
            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }
        public void Dispose()
        {
            _producer.Dispose();
        }

        public async Task PublishAsync(string topic, string key, IntegrationEvent message, CancellationToken cancellationToken = default)
        {
            var payload = JsonSerializer.Serialize(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            await PublishRawAsync(topic, key, payload, message.Header, cancellationToken);
            _logger.LogInformation("Published event to {Topic}", topic);
        }

        public Task PublishRawAsync(string topic, string key, string jsonPayload, EventHeader header, CancellationToken cancellationToken = default)
        {
            return _producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = jsonPayload }, cancellationToken);
        }
    }
}

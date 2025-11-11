
using Confluent.Kafka;
using DeliveryService.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

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
            }
        }
    }
}

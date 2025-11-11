using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatService.Infrastructure.Outbox
{
    public sealed class OutboxDispatcher : BackgroundService, IOutboxDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventBus _eventBus;
        private readonly ILogger<OutboxDispatcher> _logger;

        public OutboxDispatcher(
            IServiceProvider serviceProvider,
            IEventBus eventBus,
            ILogger<OutboxDispatcher> logger)
        {
            _serviceProvider = serviceProvider;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task DispatchPendingAsync(IOutboxStore _outboxStore, CancellationToken cancellationToken = default)
        {
            var batch = await _outboxStore.DequeueBatchAsync(100, cancellationToken);
            var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var msg in batch)
            {
                try
                {
                    var json = msg.Payload;
                    var convId = ExtractConversationId(json);
                    var topic = msg.Type;
                    EventHeader? header = null;
                    if (!string.IsNullOrWhiteSpace(msg.Headers))
                    {
                        header = JsonSerializer.Deserialize<EventHeader>(msg.Headers, jsonOpts);
                    }

                    await _eventBus.PublishRawAsync(topic, convId, json, header!, cancellationToken);

                    await _outboxStore.MarkDispatchedAsync(msg.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispatch outbox message {MessageId}", msg.Id);
                    await _outboxStore.MarkFailedAsync(msg.Id, ex.Message);
                }
            }
        }

        private static string ExtractConversationId(string json)
        {
            var marker = "\"conversationId\":\"";
            var idx = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return "unknown";
            var start = idx + marker.Length;
            var end = json.IndexOf('"', start);
            return end > start ? json[start..end] : "unknown";
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                   
                    var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

                    
                    await DispatchPendingAsync(outboxStore, stoppingToken);
                }
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
            }
        }
    }
}

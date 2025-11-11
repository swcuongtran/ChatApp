namespace BuildingBlock.Messaging
{
    public interface IEventBus
    {
        Task PublishAsync(string topic, string key, IntegrationEvent message, CancellationToken cancellationToken = default);
        Task PublishRawAsync(string topic, string key, string jsonPayload, EventHeader header, CancellationToken cancellationToken = default);
    }
}

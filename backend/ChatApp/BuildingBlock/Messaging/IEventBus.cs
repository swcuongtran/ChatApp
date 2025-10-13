namespace BuildingBlock.Messaging
{
    public interface IEventBus
    {
        Task PublishAsync(string topic, string key, IntegrationEvent message, CancellationToken cancellationToken = default);
    }
}

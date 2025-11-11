namespace BuildingBlock.Outbox
{
    public interface IOutboxDispatcher
    {
        Task DispatchPendingAsync(IOutboxStore outboxStore,CancellationToken cancellationToken = default);
    }
}

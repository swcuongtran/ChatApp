namespace BuildingBlock.Outbox
{
    public interface IOutboxDispatcher
    {
        Task DispatchPendingAsync(CancellationToken cancellationToken = default);
    }
}

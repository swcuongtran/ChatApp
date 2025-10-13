namespace BuildingBlock.Outbox
{
    public interface IOutboxStore
    {
        Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OutboxMessage>> DequeueBatchAsync(int maxCount, CancellationToken cancellationToken = default);\
        Task MarkDispatchedAsync(string messageId, CancellationToken cancellationToken = default);
        Task MarkFailedAsync(string messageId, string error, CancellationToken cancellationToken = default);
    }
}

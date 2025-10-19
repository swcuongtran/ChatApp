using BuildingBlock.DomainBase;

namespace BuildingBlock.Repositories
{
    public interface IReadRepository<TAggregate, TId> where TAggregate : AggregateRoot<TId>
    {
        Task<TAggregate>? GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<bool> ExistAsync(TId id, CancellationToken cancellationToken = default);
    }
}

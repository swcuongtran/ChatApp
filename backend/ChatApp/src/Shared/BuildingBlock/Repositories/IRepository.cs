using BuildingBlock.DomainBase;

namespace BuildingBlock.Repositories
{
    public interface IRepository<TAggregate, TId> : IReadRepository<TAggregate, TId>
        where TAggregate : AggregateRoot<TId>
    {
        Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
        Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
        Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    }
}

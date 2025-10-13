namespace BuildingBlock.DomainBase
{
    public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        public long Version { get; protected set; } 

        protected AggregateRoot() { }
        protected AggregateRoot(TId id) : base(id) { }

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}

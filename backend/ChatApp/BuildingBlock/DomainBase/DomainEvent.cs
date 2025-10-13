namespace BuildingBlock.DomainBase
{
    public interface IDomainEvent
    {
        DateTimeOffset OccurredAt { get; }
    }

    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();
    }

    public abstract record DomainEventBase(DateTimeOffset OccurredAt) : IDomainEvent;
}

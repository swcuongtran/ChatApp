namespace BuildingBlock.Messaging
{
    public abstract record IntegrationEvent(string EventId, DateTimeOffset OccurredAt, EventHeader Header);
    public sealed record IntegrationEvent<TData>(
    string EventId,
    DateTimeOffset OccurredAt,
    EventHeader Headers,
    TData Data
) : IntegrationEvent(EventId, OccurredAt, Headers);
}

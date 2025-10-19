namespace BuildingBlock.Messaging
{
    public abstract record IntegrationEvent(string EventId, DateTimeOffset OccurredAt, EventHeader Header);
}

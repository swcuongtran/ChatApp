namespace BuildingBlock.Messaging
{
    public sealed record EventHeader(string SchemaVersion, string Producer, string TraceId, string CorrelationId);
}

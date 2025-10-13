namespace BuildingBlock.Outbox
{
    public sealed class OutboxMessage
    {
        public string Id { get; init; } = null!;
        public string Type { get; init; } = null!;
        public string Payload { get; init; } = null!;
        public DateTimeOffset OccurredAt { get; init; }
        public string? Headers { get; init; }
        public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
        public int AttemptCount { get; set; }
        public DateTimeOffset? LastAttemptAt { get; set; }
        public string? LastError { get; set; }
    }
}

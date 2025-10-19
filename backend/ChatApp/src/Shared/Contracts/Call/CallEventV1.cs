namespace Contracts.Call
{
    public enum CallEventType
    {
        Started,
        Ended,
        Joined,
        Left,
        Muted,
        Unmuted
    }
    public sealed record CallEventV1(
        string CallId,
        string ConversationId,
        string UserId,
        CallEventType EventType,
        DateTimeOffset OccurredAtUtc
    );
}

namespace Contracts.Presence
{
    public enum TypingEventType
    {
        Started,
        Stopped
    }
    public sealed record TypingEventV1(
        string UserId,
        string ConversationId,
        TypingEventType Type,
        DateTimeOffset EventAtUtc
    );
}

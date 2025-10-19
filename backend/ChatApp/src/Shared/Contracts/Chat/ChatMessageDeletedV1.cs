namespace Contracts.Chat
{
    public sealed record ChatMessageDeletedV1
    (
        string MessageId,
        string ConversationId,
        string ActorUserId,
        bool HardDelete,
        DateTimeOffset DeletedAtUtc
    );
}

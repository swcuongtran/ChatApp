namespace Contracts.Chat
{
    public sealed record ChatMessageEditedV1(
        string MessageId,
        string ConversationId,
        string EditorUserId,
        string NewContent,
        DateTimeOffset EditedAtUtc
    );
}

namespace Contracts.Chat
{
    public sealed record ChatMessageCreatedV1
    (
        string MessageId,
        string ConversationId,
        string SenderId,
        string Content,
        IReadOnlyList<string?> AttachmentIds,
        DateTimeOffset CreatedAtUtc
    );
}

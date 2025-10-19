namespace Contracts.Chat
{
    public sealed record ConversationCreatedV1
    (
        string ConversationId,
        bool IsDirect,
        string CreatedByUserId,
        IReadOnlyList<string> Members,
        string? Title,
        DateTimeOffset CreatedAtUtc
    );
}

namespace Contracts.Chat
{
    public sealed record ConversationRenamedV1(
    string ConversationId,
    string NewTitle,
    string ActorUserId,
    DateTimeOffset OccurredAtUtc
);
}

namespace Contracts.Chat
{
    public sealed record ConversationMembersChangedV1(
    string ConversationId,
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    string ActorUserId,
    DateTimeOffset OccurredAtUtc
);
}

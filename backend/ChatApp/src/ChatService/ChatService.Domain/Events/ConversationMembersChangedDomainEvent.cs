using BuildingBlock.DomainBase;

namespace ChatService.Domain.Events
{
    public sealed record ConversationMembersChangedDomainEvent(
    DateTimeOffset OccurredAt,
    string ConversationId,
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    string ActorUserId
) : DomainEventBase(OccurredAt);
}

using BuildingBlock.DomainBase;

namespace ChatService.Domain.Events
{
    public sealed record ConversationRenamedDomainEvent(
    DateTimeOffset OccurredAt,
    string ConversationId,
    string NewTitle,
    string ActorUserId
) : DomainEventBase(OccurredAt);
}

using BuildingBlock.DomainBase;

namespace ChatService.Domain.Events
{
    public sealed record MessageSentDomainEvent(
        DateTimeOffset SentAt,
        string MessageId,
        string ConversationId,
        string SenderId,
        string Content
    ): DomainEventBase(SentAt);
}

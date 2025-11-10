using BuildingBlock.CQRS;

namespace ChatService.Application.Conversations
{
    public sealed record AddConversationMemberCommand
    (
        string ConversationId,
        string ActorUserId,
        IEnumerable<string> NewMemberUserIds,
        string? TraceId = null,
        string? CorrelationId = null
    ) : ICommand;
}

using BuildingBlock.CQRS;

namespace ChatService.Application.Conversations
{
    public sealed record RemoveConversationMemberCommand
    (
        string ConversationId,
        string ActorUserId,
        IEnumerable<string> MemberUserIdsToRemove,
        string? TraceId = null,
        string? CorrelationId = null
    ) : ICommand;
}

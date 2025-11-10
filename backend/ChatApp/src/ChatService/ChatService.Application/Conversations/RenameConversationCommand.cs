using BuildingBlock.CQRS;

namespace ChatService.Application.Conversations
{
    public sealed record RenameConversationCommand(string ConversationId, string ActorUserId, string NewTitle, string? TraceId, string? CorrelationId) : ICommand;
        };

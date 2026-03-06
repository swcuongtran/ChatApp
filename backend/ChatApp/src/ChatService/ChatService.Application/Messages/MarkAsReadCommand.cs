using BuildingBlock.CQRS;

namespace ChatService.Application.Messages
{
    public record MarkAsReadCommand(string ConversationId,
        string UserId,
        string TraceId,
        string CorrelationId
    ) : ICommand<string>;
}

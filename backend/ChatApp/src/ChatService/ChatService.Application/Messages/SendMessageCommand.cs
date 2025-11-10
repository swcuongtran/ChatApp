using BuildingBlock.CQRS;

namespace ChatService.Application.Messages
{
    public sealed record SendMessageCommand
        (
        string ConversationId,
        string SenderId,
        string Content,
        string? MessageId = null,
        string? TraceId = null,
        string? CorrelationId = null
        ) : ICommand<string>;
}

using BuildingBlock.CQRS;

namespace ChatService.Application.Conversations
{
    public sealed record CreateConversationCommand
    (
        bool IsDirect,
        IReadOnlyList<string> Members,
        string? Title,
        string? IdempotencyKey,
        string? TraceId = null,
        string? CorrelationId = null
        ) : ICommand<string>;
}

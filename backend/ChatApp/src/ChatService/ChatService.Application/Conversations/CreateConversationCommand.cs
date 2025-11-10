using BuildingBlock.CQRS;

namespace ChatService.Application.Conversations
{
    public sealed record CreateConversationCommand
    (
        bool IsDirect,
        IReadOnlyList<string> Members,
        string? Title,
        string? IdempotencyKey
        ) : ICommand<string>;
}

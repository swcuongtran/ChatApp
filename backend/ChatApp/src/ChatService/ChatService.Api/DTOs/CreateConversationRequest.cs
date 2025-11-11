namespace ChatService.Api.DTOs
{
    public sealed record CreateConversationRequest(
        bool IsDirect,
        IReadOnlyList<string> Members,
        string? Title,
        string? IdempotencyKey = null
    );
}

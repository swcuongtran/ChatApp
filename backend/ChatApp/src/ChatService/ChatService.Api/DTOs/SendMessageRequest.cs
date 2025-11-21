namespace ChatService.Api.DTOs
{
    public sealed record SendMessageRequest
    (
        string ConversationId,
        string Content,
        string? MessageId = null
    );
}

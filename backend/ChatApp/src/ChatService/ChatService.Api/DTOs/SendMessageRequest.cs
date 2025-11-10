namespace ChatService.Api.DTOs
{
    public sealed record SendMessageRequest
    (
        string ConversationId,
        string SenderId,
        string Content,
        string? MessageId = null
    );
}

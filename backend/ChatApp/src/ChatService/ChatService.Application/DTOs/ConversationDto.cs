namespace ChatService.Application.DTOs
{
    public record ConversationDto(
         string Id,
         string Title,
         bool IsDirect,
         List<string> Members,
         string? LastMessageContent,
         string? LastMessageSenderId,
         DateTimeOffset? LastMessageSentAt
     );
}

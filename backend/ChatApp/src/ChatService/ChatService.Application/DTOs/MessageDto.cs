namespace ChatService.Application.DTOs
{
    public record MessageDto(
        string Id,
        string SenderId,
        string Content,
        DateTimeOffset SentAt
    );
}

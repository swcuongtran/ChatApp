namespace Contracts.Chat
{
    public sealed record UserReadMessageV1(
        string ConversationId,
        string UserId,
        DateTimeOffset ReadAtUtc
    );
}

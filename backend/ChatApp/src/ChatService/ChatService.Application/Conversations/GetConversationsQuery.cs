using BuildingBlock.CQRS;
using ChatService.Application.DTOs;
namespace ChatService.Application.Conversations
{
    public record GetConversationsQuery(string UserId) : IQuery<IReadOnlyList<ConversationDto>>;
}

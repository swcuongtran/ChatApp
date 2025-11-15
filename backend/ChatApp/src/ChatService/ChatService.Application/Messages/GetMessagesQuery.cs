using BuildingBlock.CQRS;
using ChatService.Application.DTOs;

namespace ChatService.Application.Messages
{
    public record GetMessagesQuery(string ConversationId, int Skip = 0, int Take = 20)
        : IQuery<IReadOnlyList<MessageDto>>;
}

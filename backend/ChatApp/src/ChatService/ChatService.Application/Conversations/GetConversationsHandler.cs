using BuildingBlock.CQRS;
using ChatService.Application.Abstractions;
using ChatService.Application.DTOs;
using System.Linq;

namespace ChatService.Application.Conversations
{
    public class GetConversationsHandler : IQueryHandler<GetConversationsQuery, IReadOnlyList<ConversationDto>>
    {
        private readonly IConversationRepository _conversationRepository;
        public GetConversationsHandler(IConversationRepository conversationRepository)
        {
            _conversationRepository = conversationRepository;
        }
        public async Task<IReadOnlyList<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
        {
            var conversations = await _conversationRepository.GetByUserIdAsync(request.UserId);
            return conversations.Select(c =>
            {
                var lastMsg = c.Messages.FirstOrDefault();
                return new ConversationDto(
                    Id: c.Id,
                    Title: c.Title,
                    IsDirect: c.IsDirect,
                    Members: c.Members.ToList(),
                    LastMessageContent: lastMsg?.Content,
                    LastMessageSenderId: lastMsg?.SenderId,
                    LastMessageSentAt: lastMsg?.SentAt
                );
            }).ToList();
        }
    }
}

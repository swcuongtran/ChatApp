using BuildingBlock.CQRS;
using ChatService.Application.Abstractions;
using ChatService.Application.DTOs;

namespace ChatService.Application.Messages
{
    public class GetMessagesHandler : IQueryHandler<GetMessagesQuery, IReadOnlyList<MessageDto>>
    {
        private readonly IConversationRepository _repo; 

        public GetMessagesHandler(IConversationRepository repo)
        {
            _repo = repo;
        }

        public async Task<IReadOnlyList<MessageDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
        {
            var messages = await _repo.GetMessagesAsync(request.ConversationId, request.Skip, request.Take, cancellationToken);

            var dtos = messages.Select(m => new MessageDto(
                m.Id,
                m.SenderId,
                m.Content,
                m.SentAt
            )).OrderBy(m => m.SentAt).ToList(); 

            return dtos;
        }
    }
}

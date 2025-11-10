using ChatService.Domain.Conversations;
using Microsoft.VisualBasic;

namespace ChatService.Application.Abstractions
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetAsync(string conversationId);
        Task AddAsync(Conversation conversation);
        Task UpdateAsync(Conversation conversation);
    }
}

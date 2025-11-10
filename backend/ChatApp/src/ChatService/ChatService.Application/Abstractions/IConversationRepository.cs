using ChatService.Domain.Conversations;
using Microsoft.VisualBasic;

namespace ChatService.Application.Abstractions
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetAsync(string conversationId, CancellationToken ct = default);
        Task AddAsync(Conversation conversation, CancellationToken ct = default);
        Task UpdateAsync(Conversation conversation, CancellationToken ct = default);
        Task<Conversation?> GetByDirectKeyAsync(string directKey, CancellationToken ct = default);
        Task<Conversation?> GetByIdAsync(string conversationId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string conversationId, CancellationToken ct = default);
    }
}

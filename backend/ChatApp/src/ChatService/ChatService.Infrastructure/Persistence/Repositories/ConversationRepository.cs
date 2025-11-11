using ChatService.Application.Abstractions;
using ChatService.Domain.Conversations;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Persistence.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly ChatDbContext _dbContext;
        public ConversationRepository(ChatDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Conversation conversation, CancellationToken token)
        {
            await _dbContext.conversations.AddAsync(conversation);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(string conversationId, CancellationToken ct = default)
        {
            return await _dbContext.conversations.AsNoTracking().AnyAsync(x => x.Id == conversationId);
        }

        public async Task<Conversation?> GetAsync(string conversationId, CancellationToken token)
        {
            return await _dbContext.conversations.Include(c=> c.Messages)
            .FirstOrDefaultAsync(x => x.Id == conversationId);
        }

        public async Task<Conversation?> GetByDirectKeyAsync(string directKey, CancellationToken ct = default)
        {
            return await _dbContext.conversations
                .Include(c=>c.Messages)
                .FirstOrDefaultAsync(x => x.DirectKey == directKey, ct);
        }

        public async Task<Conversation?> GetByIdAsync(string conversationId, CancellationToken ct = default)
        {
            return await GetAsync(conversationId, ct);
        }

        public async Task UpdateAsync(Conversation conversation, CancellationToken token)
        {
            _dbContext.conversations.Update(conversation);
            await _dbContext.SaveChangesAsync();
        }
    }
}

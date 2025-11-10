using ChatService.Application.Abstractions;
using ChatService.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Infrastructure.Persistence.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly ChatDbContext _dbContext;
        public ConversationRepository(ChatDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Conversation conversation)
        {
            await _dbContext.conversations.AddAsync(conversation);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Conversation?> GetAsync(string conversationId)
        {
            return await _dbContext.conversations.Include("_messages")
            .Include("_members")
            .FirstOrDefaultAsync(x => x.Id == conversationId);
        }

        public async Task UpdateAsync(Conversation conversation)
        {
            _dbContext.conversations.Update(conversation);
            await _dbContext.SaveChangesAsync();
        }
    }
}

using BuildingBlock.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Outbox
{
    public class EfOutboxStore : IOutboxStore
    {
        private readonly ChatDbContext _db;
        public EfOutboxStore(ChatDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            await _db.OutboxMessages.AddAsync(message, cancellationToken);
        }

        public async Task<IReadOnlyList<OutboxMessage>> DequeueBatchAsync(int maxCount, CancellationToken cancellationToken = default)
        {
            var items = await _db.OutboxMessages
                .Where(m => m.Status == OutboxStatus.Pending)
                .OrderBy(m => m.OccurredAt)
                .Take(maxCount)
                .ToListAsync(cancellationToken);
            return items;
        }

        public async Task MarkDispatchedAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var m = await _db.OutboxMessages.FindAsync([messageId], cancellationToken);
            if (m is null) return;
            m.Status = OutboxStatus.Dispatched;
            m.LastAttemptAt = DateTimeOffset.UtcNow;
            m.AttemptCount += 1;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkFailedAsync(string messageId, string error, CancellationToken cancellationToken = default)
        {
            var m = await _db.OutboxMessages.FindAsync([messageId], cancellationToken);
            if (m is null) return;
            m.Status = OutboxStatus.Failed;
            m.LastAttemptAt = DateTimeOffset.UtcNow;
            m.AttemptCount += 1;
            m.LastError = error;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

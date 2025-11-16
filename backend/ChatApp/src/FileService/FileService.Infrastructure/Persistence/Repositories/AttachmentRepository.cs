using FileService.Domain.Aggregate;
using FileService.Domain.Repositories;

namespace FileService.Infrastructure.Persistence.Repositories
{
    public class AttachmentRepository : IAttachmentRepository
    {
        private readonly FileDbContext _db;
        public AttachmentRepository(FileDbContext db)
        {
            _db = db;
        }
        public async Task AddAsync(Attachment aggregate, CancellationToken cancellationToken = default)
        {
            await _db.Attachments.AddAsync(aggregate, cancellationToken);
        }

        public async Task DeleteAsync(Attachment aggregate, CancellationToken cancellationToken = default)
        {
            _db.Attachments.Remove(aggregate);
            await Task.CompletedTask;
        }

        public Task<bool> ExistAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Attachment>? GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task SaveChangeAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public Task UpdateAsync(Attachment aggregate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

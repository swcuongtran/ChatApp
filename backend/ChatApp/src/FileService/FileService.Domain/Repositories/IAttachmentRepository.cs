using BuildingBlock.Repositories;
using FileService.Domain.Aggregate;

namespace FileService.Domain.Repositories
{
    public interface IAttachmentRepository : IRepository<Attachment,string>
    {
        Task SaveChangeAsync(CancellationToken cancellationToken = default);
    }
}

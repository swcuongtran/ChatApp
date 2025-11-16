using BuildingBlock.Outbox;
using FileService.Domain.Aggregate;
using Microsoft.EntityFrameworkCore;
namespace FileService.Infrastructure
{
    public class FileDbContext : DbContext
    {
        public DbSet<Attachment> Attachments => Set<Attachment>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Attachment>(b => {
                b.HasKey(x => x.Id);
                b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            });

            modelBuilder.Entity<OutboxMessage>(b => {
                b.HasKey(x => x.Id);
                b.Property(x => x.Payload).IsRequired();
            });
            base.OnModelCreating(modelBuilder);
        }
    }
}

using BuildingBlock.Outbox;
using ChatService.Domain.Conversations;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options)
            : base(options)
        {
        }

        public DbSet<Conversation> conversations => Set<Conversation>();
        public DbSet<Message> messages => Set<Message>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
            modelBuilder.Entity<OutboxMessage>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).IsRequired();
                e.Property(x => x.Payload).IsRequired();
                e.Property(x => x.Status).HasConversion<int>();
            });
        }
    }
}

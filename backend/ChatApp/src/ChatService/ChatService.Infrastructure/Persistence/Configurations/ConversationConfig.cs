using ChatService.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Infrastructure.Persistence.Configurations
{
    public sealed class ConversationConfig : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.IsDirect);
            builder.Property(x => x.Title);
            builder.Property(x => x.DirectKey).HasMaxLength(200);
            builder.HasIndex(x => x.DirectKey).IsUnique();
            builder.OwnsMany(c => c.Messages, mb =>
            {
                mb.ToTable("messages");
                mb.HasKey(x => x.Id);
                mb.Property(x => x.ConversationId).IsRequired();
                mb.Property(x => x.SenderId).IsRequired();
                mb.Property(x => x.Content).IsRequired();
                mb.Property(x => x.SentAt).IsRequired();
                mb.WithOwner().HasForeignKey("ConversationId");
                mb.HasIndex(x => new { x.ConversationId, x.SentAt });
            });


            builder.ToTable("conversations");
        }
    }
}

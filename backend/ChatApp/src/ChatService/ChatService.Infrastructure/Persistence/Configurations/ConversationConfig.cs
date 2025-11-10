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
            builder.Metadata.FindNavigation(nameof(Conversation.Messages))!.SetPropertyAccessMode(PropertyAccessMode.Field);
            builder.OwnsMany<Message>("_messages", mb =>
            {
                mb.ToTable("Messages");
                mb.HasKey(x => x.Id);
                mb.Property(x => x.ConversationId).IsRequired();
                mb.Property(x => x.SenderId).IsRequired();
                mb.Property(x => x.Content).IsRequired();
                mb.Property(x => x.SentAt).IsRequired();
                mb.WithOwner().HasForeignKey("ConversationId");
                mb.HasIndex(x => new { x.ConversationId, x.SentAt });
            });

            builder.OwnsMany<string>("_members", mb =>
            {
                mb.ToTable("ConversationMembers");
                mb.WithOwner().HasForeignKey("ConversationId");
                mb.Property<string>("Value").HasColumnName("UserId").IsRequired();
                mb.HasKey("ConversationId", "UserId");
                mb.HasIndex("UserId");
            });
            builder.ToTable("Conversations");
        }
    }
}

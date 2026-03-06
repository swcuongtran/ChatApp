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
            builder.ToTable("conversations");

            builder.Property(x => x.IsDirect);
            builder.Property(x => x.Title);
            builder.Property(x => x.DirectKey).HasMaxLength(200);
            builder.HasIndex(x => x.DirectKey).IsUnique();

            // Cấu hình truy cập vào field private _conversationMembers
            builder.Navigation(c => c.ConversationMembers)
                   .UsePropertyAccessMode(PropertyAccessMode.Field);

            // 1. Cấu hình bảng thành viên
            builder.OwnsMany(c => c.ConversationMembers, mb =>
            {
                mb.ToTable("conversation_members");

                // Khóa chính kết hợp
                mb.HasKey(x => new { x.UserId, x.ConversationId });

                mb.Property(x => x.UserId).IsRequired();
                mb.Property(x => x.LastReadUtc).IsRequired();

                mb.WithOwner().HasForeignKey("ConversationId");
            });

            // 2. Cấu hình Messages
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
        }
    }
}

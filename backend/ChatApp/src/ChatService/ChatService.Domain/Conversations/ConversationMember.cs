namespace ChatService.Domain.Conversations
{
    public class ConversationMember
    {
        public string UserId { get; private set; }      // ID của người dùng
        public string ConversationId { get; private set; } // ID của hội thoại (Nên thêm)
        public DateTimeOffset LastReadUtc { get; private set; } // Mốc đã đọc

        // Constructor cập nhật
        public ConversationMember(string userId, string conversationId, DateTimeOffset lastReadUtc)
        {
            UserId = userId;
            ConversationId = conversationId;
            LastReadUtc = lastReadUtc;
        }

        public void UpdateLastRead(DateTimeOffset timestamp)
        {
            if (timestamp > LastReadUtc) LastReadUtc = timestamp;
        }
    }
}
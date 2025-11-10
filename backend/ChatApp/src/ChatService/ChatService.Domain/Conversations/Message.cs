using BuildingBlock.DomainBase;

namespace ChatService.Domain.Conversations
{
    public class Message : Entity<string>
    {
        public string ConversationId { get; private set; }
        public string SenderId { get; private set; }
        public string Content { get; private set; }
        public DateTimeOffset SentAt { get; private set; }
        private Message() { }
        public Message(string id, string conversationId, string senderId, string content, DateTimeOffset sentAt)
            : base(id)
        {
            ConversationId = conversationId;
            SenderId = senderId;
            Content = content;
            SentAt = sentAt;
        }
    }
}

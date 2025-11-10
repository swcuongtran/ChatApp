using BuildingBlock.DomainBase;
using ChatService.Domain.Events;

namespace ChatService.Domain.Conversations
{
    public class Conversation : AggregateRoot<string>
    {
        private readonly List<Message> _messages = new();
        public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

        private readonly HashSet<string> _members = new();
        public IReadOnlyCollection<string> Members => _members;

        public bool IsDirect { get; private set; }
        public string? Title { get; private set; }

        private Conversation() { }

        public Conversation(string id, IEnumerable<string> members, bool isDirect, string? title = null)
            : base(id)
        {
            _members = new HashSet<string>(members);
            IsDirect = isDirect;
            Title = title;
        }

        public Message SendMessage (string messageId, string senderId, string content, DateTimeOffset sentAt)
        {
            if (!_members.Contains(senderId))
            {
                throw new InvalidOperationException("Sender is not a member of the conversation.");
            }

            var message = new Message(messageId, Id, senderId, content, sentAt);
            _messages.Add(message);

            Raise(new MessageSentDomainEvent(sentAt, messageId, Id, senderId, content));
            return message;
        }   
    }
}

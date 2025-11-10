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
        public string? DirectKey { get; private set; }

        private Conversation() { }

        public Conversation(string id, IEnumerable<string> members, bool isDirect, string? title = null)
            : base(id)
        {
            foreach (var m in members) _members.Add(m);
            IsDirect = isDirect;
            Title = title;
        }

        public static Conversation CreateDirect(string Id, string userA, string userB, DateTimeOffset now)
        {
            var members = new[] { userA, userB };
            var conv = new Conversation(Id, members, isDirect: true, title: null);
            conv.DirectKey = BuildDirectKey(userA, userB);
            return conv;
        }

        public static string BuildDirectKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? $"{a}:{b}" : $"{b}:{a}";
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

        private void EnsureMembers(string userId)
        {
            if (!_members.Contains(userId))
            {
                throw new InvalidOperationException("User is not a member of the conversation.");
            }
        }
        public void Rename(string actorUserId, string newTitle)
        {
            EnsureMembers(actorUserId);
            if (string.IsNullOrWhiteSpace(newTitle)) throw new Exception("invalid_title,Title required.");
            Title = newTitle.Trim();
            Raise(new ConversationRenamedDomainEvent(OccurredAt: DateTimeOffset.UtcNow, ConversationId: Id, NewTitle: Title!, ActorUserId: actorUserId));
        }
        public void AddMember(string actorUserId, IEnumerable<string> newMemberUserId)
        {
            EnsureMembers(actorUserId);
            var added = new List<string>();
            foreach (var u in newMemberUserId)
                if (_members.Add(u)) added.Add(u);
            if (added.Count > 0)
                Raise(new ConversationMembersChangedDomainEvent(DateTimeOffset.UtcNow, Id, added, Removed: Array.Empty<string>(), ActorUserId: actorUserId));
        }
        public void RemoveMember(string actorUserId, IEnumerable<string> users)
        {
            EnsureMembers(actorUserId);
            var removed = new List<string>();
            foreach (var u in users)
                if (_members.Remove(u)) removed.Add(u);
            if (removed.Count > 0)
                Raise(new ConversationMembersChangedDomainEvent(DateTimeOffset.UtcNow, Id, Added: Array.Empty<string>(), removed, ActorUserId: actorUserId));
        } 

    }
}

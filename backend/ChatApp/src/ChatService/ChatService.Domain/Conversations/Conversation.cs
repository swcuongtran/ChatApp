using BuildingBlock.DomainBase;
using ChatService.Domain.Events;

namespace ChatService.Domain.Conversations
{
    public class Conversation : AggregateRoot<string>
    {
        private readonly List<ConversationMember> _conversationMembers = new();
        public IReadOnlyCollection<ConversationMember> ConversationMembers => _conversationMembers.AsReadOnly();

        private readonly List<Message> _messages = new();
        public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

        public bool IsDirect { get; private set; }
        public string? Title { get; private set; }
        public string? DirectKey { get; private set; }

        private Conversation() { }

        public Conversation(string id, IEnumerable<string> members, bool isDirect, string? title = null)
            : base(id)
        {
            // Khởi tạo thành viên kèm mốc thời gian mặc định
            foreach (var userId in members)
            {
                _conversationMembers.Add(new ConversationMember(userId,id ,DateTimeOffset.UtcNow));
            }
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

        // Logic đánh dấu đã đọc - THE KEY FOR AI SUMMARY
        public void MarkAsRead(string userId, DateTimeOffset readAt)
        {
            var member = _conversationMembers.FirstOrDefault(m => m.UserId == userId);
            if (member == null) throw new InvalidOperationException("User is not a member.");

            member.UpdateLastRead(readAt);

            // Sau khi update ở đây, tầng Application sẽ bắn Integration Event sang SearchService qua Kafka
        }

        public Message SendMessage(string messageId, string senderId, string content, DateTimeOffset sentAt)
        {
            EnsureMembers(senderId);

            var message = new Message(messageId, Id, senderId, content, sentAt);
            _messages.Add(message);

            Raise(new MessageSentDomainEvent(sentAt, messageId, Id, senderId, content));
            return message;
        }

        private void EnsureMembers(string userId)
        {
            if (_conversationMembers.All(m => m.UserId != userId))
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

        public void AddMember(string actorUserId, IEnumerable<string> newMemberUserIds)
        {
            EnsureMembers(actorUserId);
            var addedIds = new List<string>();
            foreach (var uId in newMemberUserIds)
            {
                if (_conversationMembers.All(m => m.UserId != uId))
                {
                    _conversationMembers.Add(new ConversationMember(uId,Id ,DateTimeOffset.UtcNow));
                    addedIds.Add(uId);
                }
            }
            if (addedIds.Count > 0)
                Raise(new ConversationMembersChangedDomainEvent(DateTimeOffset.UtcNow, Id, addedIds, Removed: Array.Empty<string>(), ActorUserId: actorUserId));
        }

        public void RemoveMember(string actorUserId, IEnumerable<string> userIds)
        {
            EnsureMembers(actorUserId);
            var removedIds = new List<string>();
            foreach (var uId in userIds)
            {
                var member = _conversationMembers.FirstOrDefault(m => m.UserId == uId);
                if (member != null)
                {
                    _conversationMembers.Remove(member);
                    removedIds.Add(uId);
                }
            }
            if (removedIds.Count > 0)
                Raise(new ConversationMembersChangedDomainEvent(DateTimeOffset.UtcNow, Id, Added: Array.Empty<string>(), removedIds, ActorUserId: actorUserId));
        }
    }
}
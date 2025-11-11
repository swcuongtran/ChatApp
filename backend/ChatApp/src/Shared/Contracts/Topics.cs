using BuildingBlock.Messaging;

namespace Contracts
{
    public static class Topics
    {
        public static readonly string ChatMessageCreated = TopicName.Build("chat", "message", "created", 1);
        public static readonly string ChatMessageEdited = TopicName.Build("chat", "message", "edited", 1);
        public static readonly string ChatMessageDeleted = TopicName.Build("chat", "message", "deleted", 1);
        public static readonly string ConversationCreated = TopicName.Build("chat", "conversation", "created", 1);

        public static readonly string PresenceChanged = TopicName.Build("user", "presence", "changed", 1);
        public static readonly string TypingChanged = TopicName.Build("user", "typing", "changed", 1);

        public static readonly string CallEvents = TopicName.Build("call", "session", "event", 1);

        public static readonly string AttachmentUploaded = TopicName.Build("file", "attachment", "uploaded", 1);

        public static readonly string NotificationDispatched = TopicName.Build("notification", "user", "dispatched", 1);

        public static readonly string ConversationRenamed = TopicName.Build("chat", "conversation", "renamed", 1);
        public static readonly string ConversationMembersChanged = TopicName.Build("chat", "conversation", "members_changed", 1);

        public static readonly string UserAccountCreated = TopicName.Build("user", "account", "created", 1);
        public static readonly string UserProfileUpdated = TopicName.Build("user", "profile", "updated", 1);
        public static readonly string UserAccountDeleted = TopicName.Build("user", "account", "deleted", 1);
    }
}

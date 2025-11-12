using BuildingBlock.CQRS;
using BuildingBlock.Exception;
using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using ChatService.Application.Abstractions;
using Contracts;
using Contracts.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatService.Application.Messages
{
    public sealed class SendMessageHandler : ICommandHandler<SendMessageCommand, string>
    {
        private readonly IConversationRepository _repo;
        private readonly IOutboxStore _outbox;

        public SendMessageHandler(
            IConversationRepository repo,
            IOutboxStore outbox
            )
        {
            _repo = repo;
            _outbox = outbox;
        }
        static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };  

        public async Task<string> Handle(SendMessageCommand cmd, CancellationToken cancellationToken)
        {
            Guard.AgainstNullOrWhiteSpace(cmd.ConversationId, nameof(cmd.ConversationId));
            Guard.AgainstNullOrWhiteSpace(cmd.SenderId, nameof(cmd.SenderId));
            Guard.AgainstNullOrWhiteSpace(cmd.Content, nameof(cmd.Content));
            Guard.AgainstNullOrWhiteSpace(cmd.TraceId, nameof(cmd.TraceId));
            Guard.AgainstNullOrWhiteSpace(cmd.CorrelationId, nameof(cmd.CorrelationId));


            var conv = await _repo.GetAsync(cmd.ConversationId) ?? throw new Exception($"Conversation with id {cmd.ConversationId} not found.");

            string NewId() => Guid.NewGuid().ToString("N");
            var messageId = string.IsNullOrWhiteSpace(cmd.MessageId) ? NewId() : cmd.MessageId!;
            var now = DateTimeOffset.UtcNow;

            conv.SendMessage(messageId, cmd.SenderId, cmd.Content, now);

            var data = new ChatMessageCreatedV1
            (
               MessageId: messageId,
                ConversationId: cmd.ConversationId,
                SenderId: cmd.SenderId,
                Content: cmd.Content,
                CreatedAtUtc: now,
                AttachmentIds: null
            );

            var envelope = new IntegrationEvent<ChatMessageCreatedV1>(
            EventId: NewId(),
            OccurredAt: now,
            Headers: new EventHeader(
                SchemaVersion: "1",
                Producer: "chatservice",
                TraceId: cmd.TraceId!,
                CorrelationId: cmd.CorrelationId!
            ),
            Data: data
        );
            var payloadJson = JsonSerializer.Serialize(envelope, JsonOpts);
            var headersJson = JsonSerializer.Serialize(envelope.Headers, JsonOpts);
            var outbox = new OutboxMessage
            {
                Id = envelope.EventId,
                Type = Topics.ChatMessageCreated,
                Payload = payloadJson,
                OccurredAt = now,
                Headers = headersJson,
                Status = OutboxStatus.Pending
            };

            await _outbox.AddAsync(outbox, cancellationToken);
            await _repo.UpdateAsync(conv);
            await _repo.SaveChangesAsync(cancellationToken);

            return messageId;
        }
    }
}

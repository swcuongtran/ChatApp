using BuildingBlock.CQRS;
using BuildingBlock.Exception;
using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using ChatService.Application.Abstractions;
using Contracts;
using Contracts.Chat;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace ChatService.Application.Messages
{
    public class MarkAsReadHandler : ICommandHandler<MarkAsReadCommand, string>
    {
        private readonly IConversationRepository _repo;
        private readonly IOutboxStore _outbox;

        public MarkAsReadHandler(IConversationRepository repo, IOutboxStore outbox)
        {
            _repo = repo;
            _outbox = outbox;
        }

        public async Task<string> Handle(MarkAsReadCommand cmd, CancellationToken cancellationToken)
        {
            Guard.AgainstNullOrWhiteSpace(cmd.ConversationId, nameof(cmd.ConversationId));
            Guard.AgainstNullOrWhiteSpace(cmd.UserId, nameof(cmd.UserId));

            var conversation = await _repo.GetByIdAsync(cmd.ConversationId, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            string NewId() => Guid.NewGuid().ToString("N");

            conversation.MarkAsRead(cmd.UserId, now);

            var data = new UserReadMessageV1(
                ConversationId: cmd.ConversationId,
                UserId: cmd.UserId,
                ReadAtUtc: now
            );
            var envelope = new IntegrationEvent<UserReadMessageV1>(
                EventId: NewId(),
                OccurredAt: now,
                Headers: new EventHeader(
                    SchemaVersion: "1",
                    Producer: "chatservice",
                    TraceId: cmd.TraceId ?? NewId(),
                    CorrelationId: cmd.CorrelationId ?? NewId()
                ),
                Data: data
            );

            var payloadJson = JsonSerializer.Serialize(envelope);
            var headerJson = JsonSerializer.Serialize(envelope.Headers);

            var outboxMessage = new OutboxMessage
            {
                Id = envelope.EventId,
                Type = Topics.UserReadMessage,
                Payload = payloadJson,
                Headers = headerJson,
                OccurredAt = now,
                Status = OutboxStatus.Pending
            };
            await _outbox.AddAsync(outboxMessage, cancellationToken);
            await _repo.UpdateAsync(conversation, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return conversation.Id;
        }
    }
}

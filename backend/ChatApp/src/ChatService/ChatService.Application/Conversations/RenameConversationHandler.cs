using BuildingBlock.CQRS;
using BuildingBlock.Exception;
using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using ChatService.Application.Abstractions;
using Contracts;
using Contracts.Chat;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatService.Application.Conversations
{
    public class RenameConversationHandler : ICommandHandler<RenameConversationCommand>
    {
        private readonly IConversationRepository _repo;
        private readonly IOutboxStore _outbox;
        public RenameConversationHandler(
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

        public async Task<Unit> Handle(RenameConversationCommand cmd, CancellationToken cancellationToken)
        {
            Guard.AgainstNullOrWhiteSpace(cmd.ConversationId, nameof(cmd.ConversationId));
            Guard.AgainstNullOrWhiteSpace(cmd.ActorUserId, nameof(cmd.ActorUserId));
            Guard.AgainstNullOrWhiteSpace(cmd.NewTitle, nameof(cmd.NewTitle));
            Guard.AgainstNullOrWhiteSpace(cmd.TraceId, nameof(cmd.TraceId));
            Guard.AgainstNullOrWhiteSpace(cmd.CorrelationId, nameof(cmd.CorrelationId));

            var now = DateTimeOffset.UtcNow;

            var conv = await _repo.GetAsync(cmd.ConversationId) ?? throw new Exception($"Conversation with id {cmd.ConversationId} not found.");

            conv.Rename(cmd.ActorUserId, cmd.NewTitle);

            var eventData = new ConversationRenamedV1(
                ConversationId: cmd.ConversationId,
                NewTitle: cmd.NewTitle,
                ActorUserId: cmd.ActorUserId,
                OccurredAtUtc: now
                );

            var envelope = new IntegrationEvent<ConversationRenamedV1>(
                EventId: Guid.NewGuid().ToString("N"),
                OccurredAt: now,
                Headers: new EventHeader(
                    SchemaVersion: "1",
                    Producer: "chatservice",
                    TraceId: cmd.TraceId!,
                    CorrelationId: cmd.CorrelationId!
                    ),
                Data: eventData
                );

            var outbox = new OutboxMessage
            {
                Id = envelope.EventId,
                Type = Topics.ConversationRenamed,
                Payload = JsonSerializer.Serialize(envelope, JsonOpts),
                Headers = JsonSerializer.Serialize(envelope.Headers, JsonOpts),
                OccurredAt = now
            };
            await _outbox.AddAsync(outbox, cancellationToken);

            await _repo.UpdateAsync(conv, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}

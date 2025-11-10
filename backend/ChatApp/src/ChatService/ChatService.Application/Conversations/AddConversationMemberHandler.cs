using BuildingBlock.CQRS;
using BuildingBlock.Exception;
using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using ChatService.Application.Abstractions;
using ChatService.Domain.Events;
using Contracts;
using Contracts.Chat;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatService.Application.Conversations
{
    public sealed class AddConversationMemberHandler : ICommandHandler<AddConversationMemberCommand>
    {
        private readonly IConversationRepository _repo;
        private readonly IOutboxStore _outbox;

        public AddConversationMemberHandler(IConversationRepository repo, IOutboxStore outbox)
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

        public async Task<Unit> Handle(AddConversationMemberCommand cmd, CancellationToken cancellationToken)
        {
            // 1. Validation
            Guard.AgainstNullOrWhiteSpace(cmd.ConversationId, nameof(cmd.ConversationId));
            Guard.AgainstNullOrWhiteSpace(cmd.ActorUserId, nameof(cmd.ActorUserId));
            Guard.AgainstNull(cmd.NewMemberUserIds, nameof(cmd.NewMemberUserIds));
            Guard.AgainstNullOrWhiteSpace(cmd.TraceId, nameof(cmd.TraceId));
            Guard.AgainstNullOrWhiteSpace(cmd.CorrelationId, nameof(cmd.CorrelationId));

            // 2. Get Aggregate
            var conv = await _repo.GetAsync(cmd.ConversationId, cancellationToken)
                ?? throw new Exception($"Conversation with id {cmd.ConversationId} not found.");

            // 3. Call Domain Logic
            conv.AddMember(cmd.ActorUserId, cmd.NewMemberUserIds);

            // 4. Process Domain Event
            var domainEvent = conv.DomainEvents.OfType<ConversationMembersChangedDomainEvent>().FirstOrDefault();

            if (domainEvent != null)
            {
                // 5. Create Integration Event
                var evtData = new ConversationMembersChangedV1(
                    ConversationId: conv.Id,
                    Added: domainEvent.Added,
                    Removed: domainEvent.Removed,
                    ActorUserId: cmd.ActorUserId,
                    OccurredAtUtc: domainEvent.OccurredAt
                );

                var envelope = new IntegrationEvent<ConversationMembersChangedV1>(
                    EventId: Guid.NewGuid().ToString("N"),
                    OccurredAt: domainEvent.OccurredAt,
                    Headers: new EventHeader(
                        SchemaVersion: "1",
                        Producer: "chatservice",
                        TraceId: cmd.TraceId,
                        CorrelationId: cmd.CorrelationId
                    ),
                    Data: evtData
                );

                // 6. Add to Outbox
                var outbox = new OutboxMessage
                {
                    Id = envelope.EventId,
                    Type = Topics.ConversationMembersChanged,
                    Payload = JsonSerializer.Serialize(envelope, JsonOpts),
                    Headers = JsonSerializer.Serialize(envelope.Headers, JsonOpts),
                    OccurredAt = envelope.OccurredAt
                };
                await _outbox.AddAsync(outbox, cancellationToken);
            }

            // 7. Save changes
            await _repo.UpdateAsync(conv, cancellationToken);

            return Unit.Value;
        }
    }
}

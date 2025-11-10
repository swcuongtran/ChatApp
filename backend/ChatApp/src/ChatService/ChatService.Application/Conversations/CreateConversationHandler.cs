using BuildingBlock.CQRS;
using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using ChatService.Application.Abstractions;
using ChatService.Domain.Conversations;        
using Contracts.Chat;
using System.Text.Json;

namespace ChatService.Application.Conversations
{
    public sealed class CreateConversationHandler : ICommandHandler<CreateConversationCommand, string>
    {
        private readonly IConversationRepository _repo;
        private readonly IOutboxStore _outbox;

        public CreateConversationHandler(IConversationRepository repo, IOutboxStore outbox)
        {
            _repo = repo;
            _outbox = outbox;
        }

        public async Task<string> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
        {
            // 1) Idempotency theo IdempotencyKey (nếu FE retry)
            var conversationId = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? Guid.NewGuid().ToString("N")
                : request.IdempotencyKey!;

            var existed = await _repo.GetByIdAsync(conversationId, cancellationToken);
            if (existed is not null)
                return existed.Id; 

            var now = DateTimeOffset.UtcNow;

            
            Conversation conv;
            if (request.IsDirect)
            {
                if (request.Members is null || request.Members.Count != 2)
                    throw new InvalidOperationException("Direct conversation requires exactly 2 members.");

                var key = Conversation.BuildDirectKey(request.Members[0], request.Members[1]);
                var dup = await _repo.GetByDirectKeyAsync(key, cancellationToken);
                if (dup is not null)
                    return dup.Id; // tránh tạo trùng direct

                conv = Conversation.CreateDirect(conversationId, request.Members[0], request.Members[1], now);
            }
            else
            {
                if (request.Members is null || request.Members.Count < 2)
                    throw new InvalidOperationException("Group conversation requires at least 2 members.");
                if (string.IsNullOrWhiteSpace(request.Title))
                    throw new InvalidOperationException("Group title is required.");

                conv = new Conversation(conversationId, request.Members, isDirect: false, title: request.Title!.Trim());
            }

            // 3) Lưu write model
            await _repo.AddAsync(conv, cancellationToken);

            // 4) Ghi Outbox: ConversationCreatedV1
            var evtData = new ConversationCreatedV1(
                ConversationId: conv.Id,
                IsDirect: conv.IsDirect,
                CreatedByUserId: request.Members[0],
                Members: request.Members,
                Title: conv.Title,
                CreatedAtUtc: now
            );

            var envelope = new IntegrationEvent<ConversationCreatedV1>(
                EventId: Guid.NewGuid().ToString("N"),
                OccurredAt: now,
                Headers: new EventHeader(
                    SchemaVersion: "1",
                    Producer: "chatservice",
                    TraceId: Guid.NewGuid().ToString("N"),
                    CorrelationId: Guid.NewGuid().ToString("N")
                ),
                Data: evtData
            );

            var outbox = new OutboxMessage
            {
                Id = envelope.EventId,
                Type = "chat.conversation.created.v1", // hoặc dùng Topics.ConversationCreated nếu bạn có hằng số
                Payload = JsonSerializer.Serialize(envelope),
                Headers = JsonSerializer.Serialize(envelope.Headers),
                OccurredAt = now
            };

            await _outbox.AddAsync(outbox, cancellationToken);

            return conv.Id;
        }
    }
}

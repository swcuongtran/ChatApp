using BuildingBlock.CQRS;
using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using Contracts;
using Contracts.Files;
using FileService.Application.Abstractions;
using FileService.Domain.Aggregate;
using FileService.Domain.Repositories;
using System.Text.Json;

namespace FileService.Application.Commands
{
    public class UploadFileHandler : ICommandHandler<UploadFileCommand, UploadFileResult>
    {
        private readonly IStorageService _storage;
        private readonly IAttachmentRepository _repo;
        private readonly IOutboxStore _outbox;

        public UploadFileHandler(IStorageService storage, IAttachmentRepository repo, IOutboxStore outbox)
        {
            _storage = storage;
            _repo = repo;
            _outbox = outbox;
        }
        public async Task<UploadFileResult> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            var storageKey = await _storage.UploadAsync(request.FileStream, request.FileName, request.ContentType, cancellationToken);
            var attachment = new Attachment(
                Guid.NewGuid().ToString("N"),
                request.FileName,
                request.ContentType,
                request.Size,
                storageKey,
                request.UserId,
                request.ConversationId
                );
            await _repo.AddAsync(attachment, cancellationToken);

            var now = DateTimeOffset.UtcNow;

            var evt = new AttachmentUploadedV1(
            AttachmentId: attachment.Id,
            UploadedByUserId: attachment.UploadedByUserId,
            ConversationId: attachment.ConversationId,
            FileName: attachment.FileName,
            SizeInBytes: attachment.SizeInBytes,
            ContentType: attachment.ContentType,
            ScanPassed: true,
            UploadedAtUtc: now
        );
            var envelope = new IntegrationEvent<AttachmentUploadedV1>(
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow,
                new EventHeader("1", "fileservice", null, null),
                evt
            );
            var outboxMessage = new OutboxMessage
            {
                Id = envelope.EventId,
                Type = Topics.AttachmentUploaded,
                Payload = JsonSerializer.Serialize(envelope),
                Headers = JsonSerializer.Serialize(envelope.Headers),
                OccurredAt = DateTimeOffset.UtcNow,
                Status = OutboxStatus.Pending
            };
            await _outbox.AddAsync(outboxMessage, cancellationToken);

            await _repo.SaveChangeAsync(cancellationToken);

            var url = _storage.GetPresignedUrl(storageKey);
            return new UploadFileResult(attachment.Id, url);
        }
    }
}

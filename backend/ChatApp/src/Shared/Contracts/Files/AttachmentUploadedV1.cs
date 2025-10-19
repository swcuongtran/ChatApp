namespace Contracts.Files
{
    public sealed record AttachmentUploadedV1
    (
        string AttachmentId,
        string UploadedByUserId,
        string? ConversationId,
        string FileName,
        long SizeInBytes,
        string ContentType,
        bool ScanPassed,
        DateTimeOffset UploadedAtUtc
    );
}

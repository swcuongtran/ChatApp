using BuildingBlock.CQRS;

namespace FileService.Application.Commands
{
    public record UploadFileResult(string AttachmentId, string Url);
    public record UploadFileCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long Size,
    string UserId,
    string? ConversationId
) : ICommand<UploadFileResult>;
}

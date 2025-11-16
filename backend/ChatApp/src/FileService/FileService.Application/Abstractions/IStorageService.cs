namespace FileService.Application.Abstractions
{
    public interface IStorageService
    {
        Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct);
        string GetPresignedUrl(string key);
    }
}

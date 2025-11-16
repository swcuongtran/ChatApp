using Amazon.S3;
using Amazon.S3.Model;
using FileService.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace FileService.Api.Services
{
    public class S3StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        public S3StorageService(IAmazonS3 s3, IConfiguration config)
        {
            _s3 = s3;
            _bucket = config["S3:BucketName"] ?? "chatapp-uploads";
        }
        public string GetPresignedUrl(string key)
        {
            return _s3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(24)
            });
        }

        public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct)
        {
            var key = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}-{fileName}";
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                AutoCloseStream = false
            }, ct);
            return key;
        }
    }
}

using BuildingBlock.DomainBase;

namespace FileService.Domain.Aggregate
{
    public class Attachment : AggregateRoot<string>
    {
        public string FileName { get; private set; }
        public string ContentType { get; private set; } 
        public long SizeInBytes { get; private set; }
        public string StoragePath { get; private set; } 
        public string UploadedByUserId { get; private set; }
        public string? ConversationId { get; private set; } 

        private Attachment() { } 

        public Attachment(string id,
            string fileName,
            string contentType,
            long size,
            string storagePath,
            string userId,
            string? conversationId
            ) : base(id)
        {
            if (size <= 0) throw new ArgumentException("File size must be greater than 0");
            FileName = fileName;
            ContentType = contentType;
            SizeInBytes = size;
            StoragePath = storagePath;
            UploadedByUserId = userId;
            ConversationId = conversationId;
        }

        public static Attachment Create(
            string id,
            string fileName,
            string contentType,
            long size,
            string storagePath,
            string userId,
            string? conversationId)
        {
            if (size <= 0) throw new ArgumentException("File size must be greater than 0");

            return new Attachment(id)
            {
                FileName = fileName,
                ContentType = contentType,
                SizeInBytes = size,
                StoragePath = storagePath,
                UploadedByUserId = userId,
                ConversationId = conversationId,
            };
        }

        private Attachment(string id) : base(id) { }
    }
}

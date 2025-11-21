using AnalyticsService.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AnalyticsService.Infrastructure.MongoDb.Documents
{
    public class DailyUserStatDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public DateTime Date { get; set; }
        public long MessagesSent { get; set; }
        public long FilesUploaded { get; set; }
        public long StorageUsedBytes { get; set; }

        public DailyUserStat ToDomain() => new()
        {
            Id = Id,
            UserId = UserId,
            Date = Date,
            MessagesSent = MessagesSent,
            FilesUploaded = FilesUploaded,
            StorageUsedBytes = StorageUsedBytes
        };
    }
}

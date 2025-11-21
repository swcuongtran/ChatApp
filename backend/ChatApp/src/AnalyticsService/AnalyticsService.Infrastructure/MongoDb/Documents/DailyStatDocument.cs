using AnalyticsService.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AnalyticsService.Infrastructure.MongoDb.Documents
{
    public class DailyStatDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = null!;

        public DateTime Date { get; set; }
        public long TotalMessages { get; set; }
        public long TotalFiles { get; set; }
        public long TotalStorageBytes { get; set; }
        public long NewConversations { get; set; }

        public static DailyStatDocument FromDomain(DailyStatDocument domain) => new()
        {
            Id = domain.Id,
            Date = domain.Date,
            TotalMessages = domain.TotalMessages,
            TotalFiles = domain.TotalFiles,
            TotalStorageBytes = domain.TotalStorageBytes,
            NewConversations = domain.NewConversations
        };
        public DailySystemStat ToDomain() => new()
        {
            Id = Id,
            Date = Date,
            TotalMessages = TotalMessages,
            TotalFiles = TotalFiles,
            TotalStorageBytes = TotalStorageBytes,
            NewConversations = NewConversations
        };
    }
}

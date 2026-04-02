using MongoDB.Bson.Serialization.Attributes;

namespace AnalyticsService.Infrastructure.MongoDb.Documents
{
    public class UserBasketDocument
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string Id { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string UserId { get; set; }
        public DateTime Date { get; set; }
        public List<string> Categories { get; set; } = new();
    }
}

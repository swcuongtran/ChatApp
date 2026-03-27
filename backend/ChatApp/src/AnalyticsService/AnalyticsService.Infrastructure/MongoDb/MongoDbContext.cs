using AnalyticsService.Infrastructure.MongoDb.Documents;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace AnalyticsService.Infrastructure.MongoDb
{
    public interface IMongoDbContext
    {
        IMongoCollection<DailyStatDocument> DailyStats { get; }
        IMongoCollection<DailyUserStatDocument> UserDailyStats { get; }
    }
    public class MongoDbContext : IMongoDbContext
    {
        private readonly IMongoDatabase _database;
        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration["MONGO_CONNECTION_STRING"]
                ?? throw new InvalidOperationException("MONGO_CONNECTION_STRING is missing.");
            var databaseName = configuration["MONGO_DB_NAME"] ?? "chatapp_analytics";

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }
        public IMongoCollection<DailyStatDocument> DailyStats => _database.GetCollection<DailyStatDocument>("DailySystemStat");
        public IMongoCollection<DailyUserStatDocument> UserDailyStats
        => _database.GetCollection<DailyUserStatDocument>("UserDailyStats");
    }
}

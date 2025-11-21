using AnalyticsService.Infrastructure.MongoDb.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace AnalyticsService.Infrastructure.MongoDb
{
    public interface IMongoDbContext
    {
        IMongoCollection<DailyStatDocument> DailyStats { get; }
    }
    public class MongoDbContext : DbContext, IMongoDbContext
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
    }
}

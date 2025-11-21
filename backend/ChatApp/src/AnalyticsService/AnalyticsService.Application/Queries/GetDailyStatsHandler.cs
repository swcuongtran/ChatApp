using AnalyticsService.Domain.Entities;
using AnalyticsService.Infrastructure.MongoDb;
using AnalyticsService.Infrastructure.MongoDb.Documents;
using BuildingBlock.CQRS;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AnalyticsService.Application.Queries
{
    public class GetDailyStatsHandler : IQueryHandler<GetDailyStatsQuery, IReadOnlyList<DailySystemStat>>
    {
        private readonly IMongoDbContext _db;
        public GetDailyStatsHandler(IMongoDbContext db) => _db = db;

        public async Task<IReadOnlyList<DailySystemStat>> Handle(GetDailyStatsQuery request, CancellationToken ct)
        {
            var documents = await _db.DailyStats
                .Find(_ => true)
                .SortByDescending(s => s.Date)
                .Limit(request.Days)
                .ToListAsync(ct);

            return documents.Select(d => d.ToDomain()).ToList();
        }
    }

    public class GetSummaryHandler : IQueryHandler<GetSummaryQuery, SummaryResult>
    {
        private readonly IMongoDbContext _db;
        public GetSummaryHandler(IMongoDbContext db) => _db = db;

        public async Task<SummaryResult> Handle(GetSummaryQuery request, CancellationToken ct)
        {
            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "TotalMessages", new BsonDocument("$sum", "$TotalMessages") },
                    { "TotalStorageBytes", new BsonDocument("$sum", "$TotalStorageBytes") },
                    { "TotalConversations", new BsonDocument("$sum", "$NewConversations") }
                })
            };

            var results = await _db.DailyStats.AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);
            var summary = await results.FirstOrDefaultAsync(ct);

            if (summary == null) return new SummaryResult(0, 0, 0);

            return new SummaryResult(
                TotalMessages: summary["TotalMessages"].ToInt64(),
                TotalStorageBytes: summary["TotalStorageBytes"].ToInt64(),
                TotalConversations: summary["TotalConversations"].ToInt64()
            );
        }
    }
    public class GetUserDailyStatsHandler : IQueryHandler<GetUserDailyStatsQuery, IReadOnlyList<DailyUserStat>>
    {
        private readonly IMongoDbContext _db;
        public GetUserDailyStatsHandler(IMongoDbContext db) => _db = db;

        public async Task<IReadOnlyList<DailyUserStat>> Handle(GetUserDailyStatsQuery request, CancellationToken ct)
        {
            var userFilter = Builders<DailyUserStatDocument>.Filter.Eq(d => d.UserId, request.UserId);

            var documents = await _db.UserDailyStats
                .Find(userFilter)
                .SortByDescending(s => s.Date)
                .Limit(request.Days)
                .ToListAsync(ct);

            return documents.Select(d => d.ToDomain()).ToList();
        }
    }
}
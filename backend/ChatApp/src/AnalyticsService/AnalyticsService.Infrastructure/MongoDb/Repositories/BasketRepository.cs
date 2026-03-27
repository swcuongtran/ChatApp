using System.Linq;
using AnalyticsService.Application.Abstractions;
using AnalyticsService.Domain.Entities;
using AnalyticsService.Infrastructure.MongoDb.Documents;
using MongoDB.Driver;
namespace AnalyticsService.Infrastructure.MongoDb.Repositories
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IMongoCollection<UserBasketDocument> _mongoCollection;
        public BasketRepository(IMongoDatabase mongoDatabase)
        {
            _mongoCollection = mongoDatabase.GetCollection<UserBasketDocument>("user_baskets");
        }
        public async Task<UserBasket?> GetBasketAsync(string userId, DateTime date, CancellationToken cancellationToken = default)
        {
            var targetDate = date.Date;
            var document = await _mongoCollection
                .Find(x => x.UserId == userId && x.Date == targetDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (document == null) return null;

            return new UserBasket(document.Id, document.UserId, document.Date, document.Categories);
        }

        public async Task UpsertBasketAsync(UserBasket basket, CancellationToken cancellationToken = default)
        {
            var document = new UserBasketDocument
            {
                Id = basket.Id,
                UserId = basket.UserId,
                Date = basket.Date,
                Categories = basket.Categories.ToList()
            };

            var filter = Builders<UserBasketDocument>.Filter.Eq(x => x.Id, document.Id);
            await _mongoCollection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }
    }
}

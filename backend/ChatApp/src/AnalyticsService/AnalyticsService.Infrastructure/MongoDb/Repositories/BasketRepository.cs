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
        public async Task SaveBasketAsync(UserBasket basket, CancellationToken cancellationToken = default)
        {
            var document = new UserBasketDocument
            {
                Id = basket.Id,
                UserId = basket.UserId,
                Date = basket.Date,
                Categories = basket.Categories.ToList()
            };
            await _mongoCollection.InsertOneAsync(document);
        }
    }
}

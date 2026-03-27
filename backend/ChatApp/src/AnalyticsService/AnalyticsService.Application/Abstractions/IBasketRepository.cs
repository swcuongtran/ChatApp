using AnalyticsService.Domain.Entities;

namespace AnalyticsService.Application.Abstractions
{
    public interface IBasketRepository
    {
        Task<UserBasket?> GetBasketAsync(string userId, DateTime date, CancellationToken cancellationToken = default);
        Task UpsertBasketAsync(UserBasket basket, CancellationToken cancellationToken = default);
    }
}


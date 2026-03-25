using AnalyticsService.Domain.Entities;

namespace AnalyticsService.Application.Abstractions
{
    public interface IBasketRepository
    {
        Task SaveBasketAsync(UserBasket basket, CancellationToken cancellationToken = default);
    }
}

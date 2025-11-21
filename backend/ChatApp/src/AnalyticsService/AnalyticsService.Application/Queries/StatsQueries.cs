using AnalyticsService.Domain.Entities;
using BuildingBlock.CQRS;

namespace AnalyticsService.Application.Queries
{
    public record SummaryResult(long TotalMessages, long TotalStorageBytes, long TotalConversations);
    public record GetDailyStatsQuery(int Days) : IQuery<IReadOnlyList<DailySystemStat>>;
    public record GetSummaryQuery() : IQuery<SummaryResult>;
    public record GetUserDailyStatsQuery(string UserId, int Days) : IQuery<IReadOnlyList<DailyUserStat>>;
}

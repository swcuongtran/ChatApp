namespace AnalyticsService.Domain.Entities
{
    public class DailyUserStat
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public DateTime Date { get; set; }
        public long MessagesSent { get; set; }
        public long FilesUploaded { get; set; }
        public long StorageUsedBytes { get; set; }
    }
}

namespace AnalyticsService.Domain.Entities
{
    public class DailySystemStat
    {
        public string Id { get; set; } = null!; 
        public DateTime Date { get; set; }
        public long TotalMessages { get; set; }
        public long TotalFiles { get; set; }
        public long TotalStorageBytes { get; set; }
        public long NewConversations { get; set; }
    }
}

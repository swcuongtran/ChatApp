namespace AnalyticsService.Domain.Entities
{
    public class UserBasket
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public DateTime Date { get; set; }
        
        private readonly HashSet<string> _categories;
        public IReadOnlyCollection<string> Categories => _categories;
        public UserBasket(string userId, DateTime date)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            Date = date;
            _categories = new HashSet<string>();
        }
        public UserBasket(string id, string userId, DateTime date, IEnumerable<string> existingCategories)
        {
            Id = id;
            UserId = userId;
            Date = date;
            _categories = new HashSet<string>(existingCategories);
        }

        public void AddCategory(string category) {
            if (string.IsNullOrWhiteSpace(category)) return;
            _categories.Add(category); 
        }
    }
}

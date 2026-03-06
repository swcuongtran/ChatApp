namespace SearchService.Api.Model
{
    public class UserReadMarker
    {
        public string UserId { get; set; }
        public string ConversationId { get; set; }
        public DateTimeOffset LastReadUtc { get; set; }
    }
}

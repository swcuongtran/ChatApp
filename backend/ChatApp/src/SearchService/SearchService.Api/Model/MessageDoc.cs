using Nest;
namespace SearchService.Api.Model
{
    public class MessageDoc
    {
        public string Id { get; set; } = null!;
        public string ConversationId { get; set; } = null!;
        public string SenderId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTimeOffset CreatedAtUtc { get; set; }

        [DenseVector(Dimensions = 768)]
        public float[]? Embedding { get; set; }
    };
    
}
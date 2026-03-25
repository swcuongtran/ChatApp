using SearchService.Api.Model;

namespace SearchService.Api.DTOs
{
    public record ContextSegmentDto (
        string ConversationId,
        double RelevanceScore,
        MessageDoc AnchorMessage,
        List<MessageDoc> SurroundingMessages
    );
}

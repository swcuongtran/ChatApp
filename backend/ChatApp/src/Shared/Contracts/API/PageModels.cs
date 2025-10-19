namespace Contracts.API
{
    public sealed record CursorPageRequest(string? Cursor = null, int Limit = 50);
    public sealed record CursorPageResponse<T>(IReadOnlyList<T> Items, string? NextCursor);
}

namespace ChatService.Api.DTOs
{
    public sealed record UpdateMembersRequest(
        IEnumerable<string> MemberUserIds
    );
}

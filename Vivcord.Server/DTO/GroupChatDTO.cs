namespace Vivcord.Server.DTO
{
    public record GroupChatDTO(int Id, string Name, Guid AdminId, IReadOnlyList<Guid> MemberIds);
    public record CreateGroupChatDTO(string Name);
    public record GroupChatMemberDTO(int GroupChatId, Guid UserId);
}

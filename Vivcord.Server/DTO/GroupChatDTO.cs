namespace Vivcord.Server.DTO
{
    public record GroupChatDTO
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid AdminId { get; init; }
        public IReadOnlyList<Guid> MemberIds { get; init; } = [];
        public Guid VoiceRoomId { get; init; }
        public IReadOnlyList<UserProfileDTO>? Members { get; init; }
    }
    public record CreateGroupChatDTO(string Name);
    public record GroupChatMemberDTO(int GroupChatId, Guid UserId);
}

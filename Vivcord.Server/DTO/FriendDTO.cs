namespace Vivcord.Server.DTO
{
    public record FriendDTO(
        Guid Id,
        string UserName,
        string? DisplayName = null,
        string? ProfilePictureUrl = null
    );
    public record AddFriendRequest(string Username);
}

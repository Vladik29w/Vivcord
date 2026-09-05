namespace Vivcord.Server.DTO
{
    public record FriendDTO(Guid Id, string UserName, string? ProfilePictureUrl = null);
}

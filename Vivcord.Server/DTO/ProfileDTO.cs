namespace Vivcord.Server.DTO
{
    public record ChangeDisplayNameRequest(string DisplayName);

    public record ProfileDTO(string DisplayName);

    public record UpdateProfilePictureRequest(string BlobName);

    public record UserProfileDTO(Guid UserId, string UserName, string DisplayName, string? ProfilePictureUrl);
}


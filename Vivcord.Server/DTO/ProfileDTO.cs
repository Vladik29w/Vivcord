namespace Vivcord.Server.DTO
{
    public record ProfileDTO(Guid UserId, string DisplayName);

    public record UpdateProfilePictureRequest(Guid UserId, string BlobName);

    public record UserProfileDTO(Guid UserId, string DisplayName, string? ProfilePictureUrl);
}

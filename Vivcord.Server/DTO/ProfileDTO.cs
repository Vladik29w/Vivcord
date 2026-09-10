namespace Vivcord.Server.DTO
{
    public record ChangeDisplayNameRequest(string DisplayName);

    public record UpdateProfilePictureRequest(string BlobName);

    public record UserProfileDTO(
        Guid UserId,
        string UserName,
        string DisplayName,
        string? ProfilePictureUrl
    )
    {
        public string Id => UserId.ToString();
    }

    public record FindUserDTO
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string? DisplayName { get; init; }
        public string? ProfilePictureUrl { get; init; }
        public string UserName => Name;
    }
}

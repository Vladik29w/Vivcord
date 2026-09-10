namespace Vivcord.Server.DTO
{
    public record UserDTO
    {
        public string Id { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? ProfilePictureUrl { get; init; }
        public List<string> Roles { get; init; } = [];
    }

    public record RegisterDTO
    {
        public required string Name { get; init; }
        public required string Email { get; init; }
        public required string Password { get; init; }
    }

    public record LoginDTO
    {
        public required string Email { get; init; }
        public required string Password { get; init; }
    }

    public record UserTokensDTO
    {
        public required UserDTO User { get; init; }
        public required string Token { get; init; }
        public required string RefreshToken { get; init; }
    }
}

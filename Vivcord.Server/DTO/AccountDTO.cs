namespace Vivcord.Server.DTO
{
    public record UserDTO
    {
        public string Email { get; init; } = string.Empty;
        public List<string> Roles { get; init; } = new List<string>();
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
    public record RefreshTokenDTO
    {
        public int Id { get; set; }
        public required string Token { get; set; }
        public required string UserId { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; }
        public bool IsUsed { get; set; }
        public bool IsActive => !IsRevoked && !IsUsed && DateTime.UtcNow < Expires;
    }
    public record UserTokensDTO
    {
        public UserDTO User { get; init; }
        public required string Token { get; init; }
        public required string RefreshToken { get; init; }
    }

}

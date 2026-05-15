namespace Vivcord.Server.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public required string Token { get; set; }
        public required string UserId { get; set; }
        public DateTimeOffset Created { get; set; } = TimeProvider.System.GetUtcNow();
        public DateTimeOffset Expires { get; set; }
        public bool IsRevoked { get; set; }
        public bool IsUsed { get; set; }
        public bool IsActive => !IsRevoked && !IsUsed && TimeProvider.System.GetUtcNow() < Expires;
    }
}

namespace Vivcord.Server.Models
{
    public class RefreshToken
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
}

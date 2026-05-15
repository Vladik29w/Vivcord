namespace Vivcord.Server.Models
{
    public class UserMessage
    {
        public int id { get; set; }
        public string Text { get; set; } = string.Empty;
        public required string Sender { get; set; }
        public required string Target { get; set; }
        public DateTimeOffset SentAt { get; set; } = TimeProvider.System.GetUtcNow();
    }
}

namespace Vivcord.Server.Models
{
    public class PrivateMessage
    {
        public int id { get; set; }
        public string Text { get; set; } = string.Empty;
        public required Guid Sender { get; set; }
        public required Guid Target { get; set; }
        public AppUser SenderUser { get; set; } = null!;
        public AppUser TargetUser { get; set; } = null!;
        public DateTimeOffset SentAt { get; set; } = TimeProvider.System.GetUtcNow();
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; } // "image" | "video"
    }
    public class GroupMessage
    {
        public int id { get; set; }
        public string Text { get; set; } = string.Empty;
        public required Guid Sender { get; set; }
        public AppUser SenderUser { get; set; } = null!;
        public required int GroupId { get; set; }
        public GroupChat GroupChat { get; set; } = null!;
        public DateTimeOffset SentAt { get; set; } = TimeProvider.System.GetUtcNow();
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; } // "image" | "video"
    }
}

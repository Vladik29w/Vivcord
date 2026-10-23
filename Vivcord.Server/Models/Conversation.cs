using System.ComponentModel.DataAnnotations.Schema;

namespace Vivcord.Server.Models
{
    public enum MemberStatus : byte
    {
        Pending = 0,
        Joined = 1,
        Left = 2
    }

    public enum ConversationType : byte
    {
        Direct = 0,
        Group = 1
    }

    public enum RoleType : byte
    {
        Member = 0,
        Admin = 1
    }

    public class Conversation
    {
        public long Id { get; set; } // snowflake

        public ConversationType Type { get; set; }
        public string? Title { get; set; }
        public string? AvatarUrl { get; set; }
        public string? DirectPairKey { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastMessageAt { get; set; }

        public ICollection<ConversationMember> Members { get; set; } = [];
        public ICollection<Message> Messages { get; set; } = [];
    }

    public class ConversationMember
    {
        public long ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public Guid UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public RoleType Role { get; set; } = RoleType.Member;
        public MemberStatus Status { get; set; } = MemberStatus.Joined;
        public Guid? InvitedById { get; set; }

        public long? LastReadMessageId { get; set; }
        public DateTimeOffset? JoinedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class Message
    {
        public long Id { get; set; } // snowflake

        public long ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public Guid SenderId { get; set; }
        public AppUser Sender { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EditedAt { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; } // "image" | "video"
    }
}

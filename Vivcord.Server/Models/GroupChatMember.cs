namespace Vivcord.Server.Models
{
    public class GroupChatMember
    {
        public int GroupChatId { get; set; }
        public Guid UserId { get; set; }

        // Navigation properties
        public GroupChat GroupChat { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}

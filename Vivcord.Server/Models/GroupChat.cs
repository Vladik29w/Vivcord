namespace Vivcord.Server.Models
{
    public class GroupChat
    {
        public int id { get; set; }
        public string name { get; set; }
        public Guid adminId { get; set; }

        // Navigation properties
        public AppUser Admin { get; set; } = null!;
        public ICollection<GroupChatMember> Members { get; set; } = new List<GroupChatMember>();
        public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Vivcord.Server.Models
{
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(NormalizedEmail), IsUnique = true)]
    public class AppUser : IdentityUser<Guid>
    {
        [AllowNull]
        public required override string UserName { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public ICollection<AppUserFriend> Friends { get; } = new List<AppUserFriend>();
        public ICollection<GroupChatMember> GroupMemberships { get; } = new List<GroupChatMember>();
        public ICollection<GroupChat> AdminiedGroups { get; } = new List<GroupChat>();
    }
    public class AppUserFriend
    {
        public required Guid UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public required Guid FriendId { get; set; }
        public AppUser Friend { get; set; } = null!;
    }
}

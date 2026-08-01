using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Vivcord.Server.Models
{
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(NormalizedEmail), IsUnique = true)]
    public class AppUser : IdentityUser<Guid>
    {
        public string DisplayName { get; set; } = string.Empty;
        public ICollection<AppUserFriend> Friends { get; set; } = new List<AppUserFriend>();
    }
    public class AppUserFriend
    {
        public required Guid UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public required Guid FriendId { get; set; }
        public AppUser Friend { get; set; } = null!;
    }
}

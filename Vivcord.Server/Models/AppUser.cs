using Microsoft.AspNetCore.Identity;

namespace Vivcord.Server.Models
{
    public class AppUser : IdentityUser<Guid>
    {
        private string? _displayName;
        public string DisplayName
        {
            get => string.IsNullOrWhiteSpace(_displayName) ? UserName! : _displayName;
            set => _displayName = value;
        }
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

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Vivcord.Server.Models
{
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(NormalizedEmail), IsUnique = true)]
    public class AppUser : IdentityUser<Guid>//TODO: switch to uuid7 guid
    {
        [AllowNull]
        public required override string UserName { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public ICollection<ConversationMember> ConversationMemberships { get; set; } = new List<ConversationMember>();
    }
}

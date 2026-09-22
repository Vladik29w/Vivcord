using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Vivcord.Server.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetDisplayName(this ClaimsPrincipal? user)
        {
            if (user == null) return null;

            var displayName = user.FindFirstValue("displayName");
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            return user.FindFirstValue(ClaimTypes.Name)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                ?? user.Identity?.Name;
        }

        public static Guid? GetUserId(this ClaimsPrincipal? user)
        {
            if (user == null) return null;

            var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue(JwtRegisteredClaimNames.NameId);

            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }
}

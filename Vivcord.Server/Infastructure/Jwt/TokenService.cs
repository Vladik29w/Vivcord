using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Vivcord.Server.Models;

namespace Vivcord.Server.Infastructure.Jwt
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync(AppUser user);
        string GetRefreshToken();
    }
    public class TokenService : ITokenService
    {
        private readonly UserManager<AppUser> userManager;
        private readonly TimeProvider timeProvider;
        private readonly SigningCredentials credentials;
        private readonly string issuer;
        private readonly string audience;

        public TokenService(IOptions<JwtOptions> options, UserManager<AppUser> userManager, TimeProvider timeProvider)
        {
            this.userManager = userManager;
            this.timeProvider = timeProvider;

            var jwtOptions = options.Value;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
            credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            issuer = jwtOptions.VivcordServer;
            audience = jwtOptions.VivcordClient;
        }

        public async Task<string> GetTokenAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("displayName", user.DisplayName ?? string.Empty)
            };

            var roles = await userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = timeProvider.GetUtcNow().AddMinutes(10).UtcDateTime,
                SigningCredentials = credentials,
                Issuer = issuer,
                Audience = audience
            };

            var handler = new JsonWebTokenHandler();
            return handler.CreateToken(tokenDescriptor);
        }
        public string GetRefreshToken()
        {
            var randomNumber = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomNumber);
        }
    }
}

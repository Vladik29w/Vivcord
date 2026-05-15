using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Vivcord.Server.Infastructure.Jwt
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync(IdentityUser user);
        string GetRefreshToken();
    }
    public class TokenService(IConfiguration config, UserManager<IdentityUser> userManager, TimeProvider timeProvider) : ITokenService
    {
        public async Task<string> GetTokenAsync(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
            };

            var roles = await userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var secretKey = config["JwtSetting:Key"] ?? throw new InvalidOperationException("JWT Secret Key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var TokenDecs = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = timeProvider.GetUtcNow().AddMinutes(10).UtcDateTime,
                SigningCredentials = credentials,
                Issuer = config["JwtSetting:VivcordServer"],
                Audience = config["JwtSetting:VivcordClient"]
            };

            var Handler = new JwtSecurityTokenHandler();
            var token = Handler.CreateToken(TokenDecs);

            return Handler.WriteToken(token);
        }
        public string GetRefreshToken()
        {
            var randomNumber = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomNumber);
        }
    }
}

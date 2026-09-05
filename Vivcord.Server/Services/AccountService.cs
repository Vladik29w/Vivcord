using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Infastructure.Jwt;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services
{
    public interface IAccountService
    {
        Task<ErrorOr<UserTokensDTO>> UserRegister(RegisterDTO register, CancellationToken ct = default);
        Task<ErrorOr<UserTokensDTO>> UserLogin(LoginDTO login, CancellationToken ct = default);
        Task<ErrorOr<UserTokensDTO>> RefreshUserToken(string token, CancellationToken ct = default);
        Task<ErrorOr<Success>> UserLogout(string token, CancellationToken ct = default);
    }
    public class AccountService(UserManager<AppUser> manager, ITokenService tokenService, MainDbContext dbContext, TimeProvider timeProvider) : IAccountService
    {
        public async Task<ErrorOr<UserTokensDTO>> UserRegister(RegisterDTO register, CancellationToken ct = default)
        {
            var existingUser = await manager.FindByEmailAsync(register.Email);
            if (existingUser != null)
            {
                return Error.Conflict(code: "EmailAlreadyRegistered", description: "Email is already registered");
            }

            var user = new AppUser
            {
                UserName = register.Name,
                Email = register.Email,
                DisplayName = register.Name!
            };
            var res = await manager.CreateAsync(user, register.Password);
            if (!res.Succeeded)
            {
                var errors = res.Errors
                    .Select(e => Error.Validation(code: e.Code, description: e.Description))
                    .ToList();
                return errors;
            }
            var roles = new List<string> { "User", "Admin" };
            await manager.AddToRoleAsync(user, roles[0]);
            var token = await tokenService.GetTokenAsync(user);

            var refreshToken = await SetRefreshToken(user.Id, ct);
            return new UserTokensDTO
            {
                User = new UserDTO { Id = user.Id.ToString(), Email = register.Email!, DisplayName = user.DisplayName, ProfilePictureUrl = user.ProfilePictureUrl, Roles = roles },
                Token = token,
                RefreshToken = refreshToken,
            };
        }
        public async Task<ErrorOr<UserTokensDTO>> UserLogin(LoginDTO login, CancellationToken ct = default)
        {
            var user = await manager.FindByEmailAsync(login.Email);
            if (user == null)
                return Error.NotFound(code: "UserNotFound");
            var passwordValid = await manager.CheckPasswordAsync(user, login.Password);
            if (!passwordValid)
                return Error.Validation(code: "InvalidPassword");
            var roles = await manager.GetRolesAsync(user);
            var token = await tokenService.GetTokenAsync(user);
            var refreshToken = await SetRefreshToken(user.Id, ct);
            return new UserTokensDTO
            {
                User = new UserDTO { Id = user.Id.ToString(), Email = login.Email!, DisplayName = user.DisplayName, ProfilePictureUrl = user.ProfilePictureUrl, Roles = roles.ToList() },
                Token = token,
                RefreshToken = refreshToken,
            };
        }
        public async Task<ErrorOr<Success>> UserLogout(string refToken, CancellationToken ct = default)
        {
            var activeToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refToken, ct);
            if (activeToken == null)
                return Error.NotFound(code: "TokenNotFound");
            dbContext.RefreshTokens.Remove(activeToken);
            await dbContext.SaveChangesAsync(ct);
            return Result.Success;
        }
        public async Task<ErrorOr<UserTokensDTO>> RefreshUserToken(string token, CancellationToken ct = default)
        {
            var curToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && !t.IsRevoked, ct);
            if (curToken == null || curToken.Expires < timeProvider.GetUtcNow())
                return Error.Unauthorized(code: "InvalidToken");
            var user = await manager.FindByIdAsync(curToken.UserId.ToString());
            if (user == null)
                return Error.NotFound(code: "UserNotFound");
            curToken.IsUsed = true;
            dbContext.RefreshTokens.Update(curToken);
            await dbContext.SaveChangesAsync(ct);
            var roles = await manager.GetRolesAsync(user);
            var newJwt = await tokenService.GetTokenAsync(user);
            var newRefresh = await SetRefreshToken(user.Id, ct);

            var userDto = new UserDTO { Id = user.Id.ToString(), Email = user.Email!, DisplayName = user.DisplayName, ProfilePictureUrl = user.ProfilePictureUrl, Roles = roles.ToList() };
            return new UserTokensDTO
            {
                User = userDto,
                Token = newJwt,
                RefreshToken = newRefresh,
            };
        }
        private async Task<string> SetRefreshToken(Guid userId, CancellationToken ct = default)
        {
            var refString = tokenService.GetRefreshToken();
            var refToken = new Models.RefreshToken
            {
                Token = refString,
                UserId = userId,
                Created = timeProvider.GetUtcNow(),
                Expires = timeProvider.GetUtcNow().AddDays(1),
                IsRevoked = false,
                IsUsed = false,
            };
            await dbContext.RefreshTokens.AddAsync(refToken, ct);
            await dbContext.SaveChangesAsync(ct);
            return refString;
        }
    }
}

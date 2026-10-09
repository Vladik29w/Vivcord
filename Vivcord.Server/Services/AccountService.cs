using Azure;
using Azure.Communication.Email;
using ErrorOr;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Infastructure.Jwt;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services //TODO: email verification
{
    public interface IAccountService
    {
        Task<ErrorOr<UserTokensDTO>> UserRegister(RegisterDTO register, CancellationToken ct = default);
        Task<ErrorOr<UserTokensDTO>> UserLogin(LoginDTO login, CancellationToken ct = default);
        Task<ErrorOr<UserTokensDTO>> RefreshUserToken(string token, CancellationToken ct = default);
        Task<ErrorOr<Success>> UserLogout(string token, CancellationToken ct = default);
        Task<ErrorOr<UserDTO>> GetActiveUser(Guid userId, CancellationToken ct = default);
        Task<ErrorOr<Success>> ForgotPasswordEmail(string userEmail, CancellationToken ct = default);
        Task<ErrorOr<Success>> ResetPassword(ResetPasswordDTO request, CancellationToken ct = default);
        Task<ErrorOr<UserTokensDTO>> GoogleAuthLogin(string token, CancellationToken ct = default);
    }
    public class AccountService(
        UserManager<AppUser> manager,
        ITokenService tokenService,
        MainDbContext dbContext,
        TimeProvider timeProvider,
        IConfiguration? configuration = null,
        EmailClient? emailClient = null) : IAccountService
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
            await manager.AddToRoleAsync(user, "User");
            var token = await tokenService.GetTokenAsync(user);

            var refreshToken = await SetRefreshToken(user.Id, ct);
            return new UserTokensDTO
            {
                User = new UserDTO { Id = user.Id.ToString(), Email = register.Email!, DisplayName = user.DisplayName, ProfilePictureUrl = user.ProfilePictureUrl, Roles = ["User"] },
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
        public async Task<ErrorOr<UserTokensDTO>> GoogleAuthLogin(string token, CancellationToken ct = default)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = [configuration["GoogleAuth:ClientId"] ?? throw new InvalidOperationException("Null google client ID")]
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);
            }
            catch (InvalidJwtException)
            {
                return Error.Validation("InvalidToken", "The provided Google token is not valid.");
            }

            const string providerName = "Google";

            var user = await manager.FindByLoginAsync(providerName, payload.Subject);

            if (user == null)
            {
                user = await manager.FindByEmailAsync(payload.Email);

                if (user == null)
                {
                    user = new AppUser
                    {
                        UserName = payload.Email,//TODO: make it so user can write their own username
                        Email = payload.Email,
                        DisplayName = payload.Name,
                        ProfilePictureUrl = payload.Picture,
                        EmailConfirmed = payload.EmailVerified
                    };
                    var createResult = await manager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        var errors = createResult.Errors
                            .Select(e => Error.Validation(code: e.Code, description: e.Description))
                            .ToList();
                        return errors;
                    }
                    await manager.AddToRoleAsync(user, "User");
                }
                var loginInfo = new UserLoginInfo(providerName, payload.Subject, providerName);
                var addLoginResult = await manager.AddLoginAsync(user, loginInfo);

                if (!addLoginResult.Succeeded)
                {
                    return Error.Validation("LoginAssociationFailed", "Failed to login via Google account.");
                }
            }

            var roles = await manager.GetRolesAsync(user);
            var jwtToken = await tokenService.GetTokenAsync(user);
            var refreshToken = await SetRefreshToken(user.Id, ct);
            return new UserTokensDTO
            {
                User = new UserDTO { Id = user.Id.ToString(), Email = user.Email!, DisplayName = user.DisplayName, ProfilePictureUrl = user.ProfilePictureUrl, Roles = roles.ToList() },
                Token = jwtToken,
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
        public async Task<ErrorOr<UserDTO>> GetActiveUser(Guid userId, CancellationToken ct = default)
        {
            var user = await manager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Error.NotFound("UserNotFound", "User not found.");

            var roles = await manager.GetRolesAsync(user);

            return new UserDTO
            {
                Id = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Roles = roles.ToList()
            };
        }
        public async Task<ErrorOr<Success>> ForgotPasswordEmail(string userEmail, CancellationToken ct = default)
        {
            var user = await manager.FindByEmailAsync(userEmail);
            if (user == null)
                return Result.Success;

            if (emailClient == null)
            {
                return Error.Failure("EmailServiceUnavailable", "Email service is not configured.");
            }

            var token = await manager.GeneratePasswordResetTokenAsync(user);

            string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var clientBaseUrl = configuration?["ClientUrl"]
                ?? configuration?["CorsOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
                ?? "http://localhost:4200";
            string resetLink = $"{clientBaseUrl.TrimEnd('/')}/reset-password?token={encodedToken}&userId={user.Id}";

            var content = new EmailContent("Password Reset Request")
            {
                PlainText = $"You requested a password reset. Click the link to reset your password: {resetLink}",
                Html = $"<p>You requested a password reset. Click the link below to reset your password:</p><p><a href='{resetLink}'>Reset Password</a></p>"
            };

            var senderAddress = configuration?["AzureCommunicationService:SenderAddress"] ?? "no-reply@vivcord.live";

            var message = new EmailMessage(
                senderAddress: senderAddress,
                recipientAddress: userEmail,
                content: content
            );

            try
            {
                await emailClient.SendAsync(WaitUntil.Started, message, ct);
                return Result.Success;
            }
            catch (RequestFailedException ex)
            {
                return Error.Failure("EmailSendFailed", $"Failed to send email: {ex.Message}");
            }
        }
        public async Task<ErrorOr<Success>> ResetPassword(ResetPasswordDTO request, CancellationToken ct = default)
        {
            var user = await manager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                return Error.NotFound("UserNotFound", "User not found.");

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch (FormatException)
            {
                return Error.Validation("InvalidToken", "The provided token is not valid.");
            }

            var result = await manager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(e => Error.Validation(code: e.Code, description: e.Description))
                    .ToList();
                return errors;
            }

            await dbContext.RefreshTokens
            .Where(t => t.UserId == request.UserId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), ct);

            return Result.Success;
        }

        private async Task<string> SetRefreshToken(Guid userId, CancellationToken ct = default)
        {
            var refString = tokenService.GetRefreshToken();
            var refToken = new RefreshToken
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DTO;
using Vivcord.Server.Infastructure.Jwt;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [Route("[controller]")]
    public class AccountController(IAccountService accountService) : ApiMainController
    {
        [HttpPost("register")]
        public async Task<IActionResult> UserRegister(RegisterDTO register, CancellationToken ct)
        {
            var userTokensResult = await accountService.UserRegister(register, ct);

            return userTokensResult.Match(
                user => AuthLogic(user),
                errors => Problem(errors)
            );
        }

        [HttpPost("login")]
        public async Task<IActionResult> UserLogin(LoginDTO login, CancellationToken ct)
        {
            var userTokensResult = await accountService.UserLogin(login, ct);

            return userTokensResult.Match(
                user => AuthLogic(user),
                errors => Problem(errors)
            );
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var refToken = Request.Cookies["refToken"];
            if (string.IsNullOrEmpty(refToken))
                return BadRequest("Refresh token is missing.");

            var logoutResult = await accountService.UserLogout(refToken, ct);

            return logoutResult.Match(
                success =>
                {
                    Response.Cookies.Delete("refToken");
                    Response.Cookies.Delete("jwt"); 
                    return Ok();
                },
                errors => Problem(errors)
            );
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(CancellationToken ct)
        {
            var refToken = Request.Cookies["refToken"];
            if (string.IsNullOrEmpty(refToken))
                return BadRequest("Refresh token is missing.");

            var userTokensResult = await accountService.RefreshUserToken(refToken, ct);

            return userTokensResult.Match(
                user => AuthLogic(user),
                errors => Problem(errors)
            );
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetActiveUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);

            if (string.IsNullOrEmpty(email))
                return BadRequest("User email claim not found.");

            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return Ok(new UserDTO
            {
                Email = email,
                Roles = roles
            });
        }
        private IActionResult AuthLogic(UserTokensDTO user)
        {
            if (user.User == null)
                return Unauthorized();

            Response.SetCookie(user.Token);
            Response.SetRefreshCookie(user.RefreshToken); 

            return Ok(user.User);
        }
    }
}
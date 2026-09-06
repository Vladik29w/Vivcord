using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Infastructure.Jwt;
using Vivcord.Server.Models;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ProfileController(
        IProfileService profileService,
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        TimeProvider timeProvider) : ApiMainController
    {
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetUserProfile(Guid userId, CancellationToken ct)
        {
            var result = await profileService.GetUserProfile(userId, ct);
            return result.Match<IActionResult>(
                profile => Ok(profile),
                errors => Problem(detail: errors.First().Description, statusCode: StatusCodes.Status404NotFound));
        }

        [HttpPut("display-name")]
        public async Task<IActionResult> ChangeDisplayName(ChangeDisplayNameRequest request, CancellationToken ct)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.DisplayName))
                return Problem("DisplayName is required", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                var result = await profileService.ChangeUserDisplayName(currentUserId.Value, request.DisplayName, ct);
                if (result.IsError)
                    return Problem(detail: result.Errors.First().Description, statusCode: StatusCodes.Status500InternalServerError);

                var user = await userManager.FindByIdAsync(currentUserId.Value.ToString());
                if (user != null)
                {
                    user.DisplayName = request.DisplayName;
                    var token = await tokenService.GetTokenAsync(user);
                    Response.SetCookie(token, timeProvider);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("picture-upload-token")]
        public IActionResult GetProfilePictureUploadToken([FromQuery] UploadTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
                return Problem("FileName and ContentType are required", statusCode: StatusCodes.Status400BadRequest);

            var result = profileService.GetProfilePictureSasToken(request.FileName, request.ContentType);
            return result.Match(
                token => Ok(token),
                errors => Problem(detail: errors.First().Description, statusCode: StatusCodes.Status500InternalServerError));
        }

        [HttpPut("picture-url")]
        public async Task<IActionResult> UpdateProfilePictureUrl(UpdateProfilePictureRequest request, CancellationToken ct)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.BlobName))
                return Problem("BlobName is required", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                var result = await profileService.UpdateProfilePictureUrl(currentUserId.Value, request.BlobName, ct);
                return result.Match<IActionResult>(
                    success => Ok(),
                    errors => Problem(detail: errors.First().Description, statusCode: StatusCodes.Status500InternalServerError));
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private Guid? GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guid))
                return null;

            return guid;
        }
    }
}


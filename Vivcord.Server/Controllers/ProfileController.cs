using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DTO;
using Vivcord.Server.Infastructure.Jwt;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ProfileController(IProfileService profileService, TimeProvider timeProvider) : ApiMainController
    {
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetUserProfile(Guid userId, CancellationToken ct)
        {
            var result = await profileService.GetUserProfile(userId, ct);
            return result.Match(Ok, Problem);
        }

        [HttpPut("display-name")]
        public async Task<IActionResult> ChangeDisplayName(ChangeDisplayNameRequest request, CancellationToken ct)
        {
            if (CurrentUserId is not { } currentUserId)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.DisplayName))
                return Problem(Error.Validation("DisplayNameRequired", "DisplayName is required."));

            var result = await profileService.ChangeUserDisplayName(currentUserId, request.DisplayName, ct);
            return result.Match(
                token =>
                {
                    Response.SetCookie(token, timeProvider);
                    return Ok();
                },
                Problem
            );
        }

        [HttpGet("picture-upload-token")]
        public IActionResult GetProfilePictureUploadToken([FromQuery] UploadTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
                return Problem(Error.Validation("InvalidRequest", "FileName and ContentType are required."));

            var result = profileService.GetProfilePictureSasToken(request.FileName, request.ContentType);
            return result.Match(Ok, Problem);
        }

        [HttpPut("picture-url")]
        public async Task<IActionResult> UpdateProfilePictureUrl(UpdateProfilePictureRequest request, CancellationToken ct)
        {
            if (CurrentUserId is not { } currentUserId)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.BlobName))
                return Problem(Error.Validation("InvalidRequest", "BlobName is required."));

            var result = await profileService.UpdateProfilePictureUrl(currentUserId, request.BlobName, ct);
            return result.Match(_ => Ok(), Problem);
        }
    }
}


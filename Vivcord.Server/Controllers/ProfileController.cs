using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProfileController(IProfileService profileService) : ApiMainController
    {
        [HttpPut("display-name")]
        public async Task<IActionResult> ChangeDisplayName(ProfileDTO profile, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(profile.DisplayName))
                return Problem("DisplayName is required", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                var result = await profileService.ChangeUserDisplayName(profile.UserId, profile.DisplayName, ct);
                return result.Match<IActionResult>(
                    success => Ok(),
                    errors => Problem(detail: errors.First().Description, statusCode: StatusCodes.Status500InternalServerError));
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
            if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.BlobName))
                return Problem("UserId and BlobName are required", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                var result = await profileService.UpdateProfilePictureUrl(request.UserId, request.BlobName, ct);
                return result.Match<IActionResult>(
                    success => Ok(),
                    errors => Problem(detail: errors.First().Description, statusCode: StatusCodes.Status500InternalServerError));
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}

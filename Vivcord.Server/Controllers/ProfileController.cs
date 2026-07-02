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
                await profileService.ChangeUserDisplayName(profile.UserId, profile.DisplayName, ct);
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}

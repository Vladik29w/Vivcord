using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DbContext;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProfileController(IProfileService profileService) : ApiMainController
    {
        [HttpPut("display-name")]
        public async Task<IActionResult> ChangeDisplayName([FromBody] ChangeDisplayNameRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName))
                return Problem("DisplayName is required", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                await profileService.ChangeUserDisplayName(request.UserId, request.DisplayName, ct);
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }

    public record ChangeDisplayNameRequest(Guid UserId, string DisplayName);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class FriendController(IFriendService friendService) : ApiMainController
    {
        [HttpGet("list")]
        public async Task<IActionResult> GetFriendList(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var friendListResult = await friendService.GetFriendList(userIdGuid, cancellationToken);

            return friendListResult.Match(
                friendList => Ok(friendList),
                errors => Problem(errors)
            );
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToFriendList([FromBody] AddFriendRequest? request, [FromQuery] string? userNameToAdd, CancellationToken cancellationToken)
        {
            var targetUsername = request?.Username ?? userNameToAdd;
            if (string.IsNullOrWhiteSpace(targetUsername))
                return BadRequest("Username is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var addResult = await friendService.AddToFriendList(userIdGuid, targetUsername, cancellationToken);

            return addResult.Match(
                friendDto => Ok(friendDto),
                errors => Problem(errors)
            );
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFromFriendList([FromBody] RemoveFriendRequest? request, [FromQuery] string? userNameToRemove, CancellationToken cancellationToken)
        {
            var targetUsername = request?.Username ?? userNameToRemove;
            if (string.IsNullOrWhiteSpace(targetUsername))
                return BadRequest("Username is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var removeResult = await friendService.RemoveFromFriendList(userIdGuid, targetUsername, cancellationToken);

            return removeResult.Match(
                success => Ok(),
                errors => Problem(errors)
            );
        }
    }
}

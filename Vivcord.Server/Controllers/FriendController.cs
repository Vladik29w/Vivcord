using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            if (CurrentUserId is not { } userId)
                return Unauthorized();

            var result = await friendService.GetFriendList(userId, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToFriendList([FromBody] AddFriendRequest? request, [FromQuery] string? userNameToAdd, CancellationToken cancellationToken)
        {
            var targetUsername = request?.Username ?? userNameToAdd;
            if (string.IsNullOrWhiteSpace(targetUsername))
                return Problem(Error.Validation("UsernameRequired", "Username is required."));

            if (CurrentUserId is not { } userId)
                return Unauthorized();

            var result = await friendService.AddToFriendList(userId, targetUsername, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFromFriendList([FromBody] RemoveFriendRequest? request, [FromQuery] string? userNameToRemove, CancellationToken cancellationToken)
        {
            var targetUsername = request?.Username ?? userNameToRemove;
            if (string.IsNullOrWhiteSpace(targetUsername))
                return Problem(Error.Validation("UsernameRequired", "Username is required."));

            if (CurrentUserId is not { } userId)
                return Unauthorized();

            var result = await friendService.RemoveFromFriendList(userId, targetUsername, cancellationToken);
            return result.Match(_ => Ok(), Problem);
        }
    }
}

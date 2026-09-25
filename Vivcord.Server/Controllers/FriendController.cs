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
            var result = await friendService.GetFriendList(CurrentUserId!.Value, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToFriendList([FromBody] AddFriendRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return Problem(Error.Validation("UsernameRequired", "Username is required."));

            var result = await friendService.AddToFriendList(CurrentUserId!.Value, request.Username, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpDelete("remove/{username}")]
        public async Task<IActionResult> RemoveFromFriendList([FromRoute] string username, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Problem(Error.Validation("UsernameRequired", "Username is required."));

            var result = await friendService.RemoveFromFriendList(CurrentUserId!.Value, username, cancellationToken);
            return result.Match(_ => Ok(), Problem);
        }
    }
}

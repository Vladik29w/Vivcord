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
        public async Task<IActionResult> GetFriendList()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var friendListResult = await friendService.GetFriendList(userIdGuid);

            return friendListResult.Match(
                friendList => Ok(friendList),
                errors => Problem(errors)
            );
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToFriendList(string userNameToAdd)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var addResult = await friendService.AddToFriendList(userIdGuid, userNameToAdd);

            return addResult.Match(
                FriendDTO => Ok(FriendDTO),
                errors => Problem(errors)
            );
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFromFriendList(string userNameToRemove)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var removeResult = await friendService.RemoveFromFriendList(userIdGuid, userNameToRemove);

            return removeResult.Match(
                success => Ok(),
                errors => Problem(errors)
            );
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.Server.Controllers
{
    [Route("[controller]")]
    public class MessagingController(IMessagingService messagingService) : ApiMainController
    {
        [HttpGet("history/{targetUserId:guid}")]
        public async Task<IActionResult> GetChatHistory(Guid targetUserId, CancellationToken cancellationToken)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (currentUserIdStr == null || !Guid.TryParse(currentUserIdStr, out var currentUserId))
                return Unauthorized();

            var history = await messagingService.GetChatHistory(currentUserId, targetUserId, cancellationToken);
            return Ok(history);
        }

        [HttpGet("group-history/{groupId}")]
        public async Task<IActionResult> GetGroupChatHistory(int groupId, CancellationToken cancellationToken)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (currentUserIdStr == null || !Guid.TryParse(currentUserIdStr, out var currentUserId))
                return Unauthorized();

            var history = await messagingService.GetGroupChatHistory(currentUserId, groupId, cancellationToken);
            return Ok(history);
        }
    }
}

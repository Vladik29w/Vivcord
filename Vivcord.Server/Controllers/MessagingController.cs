using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.Server.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class MessagingController(IMessagingService messagingService) : ApiMainController
    {
        [HttpGet("history/{targetUserId:guid}")]
        public async Task<IActionResult> GetChatHistory(Guid targetUserId, CancellationToken cancellationToken)
        {
            if (CurrentUserId is not { } currentUserId)
                return Unauthorized();

            var history = await messagingService.GetPrivateChatHistory(currentUserId, targetUserId, cancellationToken);
            return Ok(history);
        }

        [HttpGet("group-history/{groupId}")]
        public async Task<IActionResult> GetGroupChatHistory(int groupId, CancellationToken cancellationToken)
        {
            if (CurrentUserId is not { } currentUserId)
                return Unauthorized();

            var history = await messagingService.GetGroupChatHistory(currentUserId, groupId, cancellationToken);
            return Ok(history);
        }
    }
}

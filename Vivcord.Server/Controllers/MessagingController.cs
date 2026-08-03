using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.Server.Controllers
{
    [Route("[controller]")]
    public class MessagingController(IMessagingService messagingService) : ApiMainController
    {
        [HttpGet("history/{targetUserId}")]
        public async Task<IActionResult> GetChatHistory(string targetUserId, CancellationToken cancellationToken)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId == null)
                return Unauthorized();

            var history = await messagingService.GetChatHistory(currentUserId, targetUserId, cancellationToken);
            return Ok(history);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessagingController(IMessagingService messageService) : ApiMainController
    {
        [HttpGet("find/{username}")]
        public async Task<IActionResult> GetProfileByUsername(string username)
        {
            var res = await messageService.GetProfileByUsername(username);
            if (res == null) return NotFound("User not found");
            return Ok(res);
        }
        [HttpGet("history/{targetUserId}")]
        public async Task<IActionResult> GetChatHistory(string targetUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId == null)
                return Unauthorized();

            var history = await messageService.GetChatHistory(currentUserId, targetUserId);
            return Ok(history);
        }
    }
}

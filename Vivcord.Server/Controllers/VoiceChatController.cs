using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class VoiceChatController(IVoiceChatService voiceChatService) : ApiMainController
    {
        [HttpPost("private-call")]
        public async Task<IActionResult> InitiatePrivateCall(
            [FromBody] PrivateCallRequestDTO request,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is not { } callerId)
                return Unauthorized();

            var result = await voiceChatService.InitiatePrivateCallAsync(callerId, CurrentUserDisplayName, request, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpPost("group-call")]
        public async Task<IActionResult> InitiateGroupCall(
            [FromBody] GroupCallRequestDTO request,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is not { } callerId)
                return Unauthorized();

            var result = await voiceChatService.InitiateGroupCallAsync(callerId, CurrentUserDisplayName, request, cancellationToken);
            return result.Match(Ok, Problem);
        }
    }
}

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
        [HttpPost("{conversationId:long}")]
        [HttpPost("call/{conversationId:long}")]
        [HttpPost("initiate/{conversationId:long}")]
        public async Task<IActionResult> InitiateVoiceCall(
            long conversationId,
            CancellationToken cancellationToken)
        {
            var callerId = CurrentUserId!.Value;
            var result = await voiceChatService.InitiateVoiceCall(callerId, CurrentUserDisplayName, conversationId, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpPost("call")]
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateVoiceCallFromBody(
            [FromBody] VoiceCallRequestDTO request,
            CancellationToken cancellationToken)
        {
            var callerId = CurrentUserId!.Value;
            var result = await voiceChatService.InitiateVoiceCall(callerId, CurrentUserDisplayName, request.ConversationId, cancellationToken);
            return result.Match(Ok, Problem);
        }
    }
}

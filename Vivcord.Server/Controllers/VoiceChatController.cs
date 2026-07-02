using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VoiceChatController(IVoiceChatService voiceChatService) : ApiMainController
    {
        [HttpPost("VoiceToken")]
        public IActionResult GenerateToken(VoiceTokenDTO voiceToken)
        {
            try
            {
                var token = voiceChatService.GenerateToken(voiceToken);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}

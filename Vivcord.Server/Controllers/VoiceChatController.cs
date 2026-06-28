using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VoiceChatController(IVoiceChatService voiceChatService) : ApiMainController
    {
        [HttpPost("VoiceToken")]
        public IActionResult GenerateToken([FromBody] GenerateTokenRequest request)
        {
            try
            {
                var token = voiceChatService.GenerateToken(request.RoomName, request.Identity);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
    public record GenerateTokenRequest(string RoomName, string Identity);
}

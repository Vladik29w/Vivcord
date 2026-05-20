using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContactController(IContactService messageService) : ApiMainController
    {
        [HttpGet("find/{username}")]
        public async Task<IActionResult> GetProfileByUsername(string username)
        {
            var res = await messageService.GetProfileByUsername(username);
            if (res == null) return NotFound("User not found");
            return Ok(res);
        }
    }
}

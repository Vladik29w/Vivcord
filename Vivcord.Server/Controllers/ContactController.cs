using Microsoft.AspNetCore.Mvc;
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
            var result = await messageService.GetProfileByUsername(username);
            return result.Match(Ok, Problem);
        }
    }
}

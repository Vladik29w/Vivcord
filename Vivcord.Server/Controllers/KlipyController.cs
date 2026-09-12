using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.Infastructure.Api;

namespace Vivcord.Server.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class KlipyController(IKlipyService klipyService) : ApiMainController
    {
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingGifs()
        {
            var result = await klipyService.GetTrendingGifs();
            return result.Match(
                gifs => Ok(gifs),
                errors => Problem(errors)
            );
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchGifs([FromQuery] string query)
        {
            var result = await klipyService.SearchGifs(query);
            return result.Match(
                gifs => Ok(gifs),
                errors => Problem(errors)
            );
        }
    }
}

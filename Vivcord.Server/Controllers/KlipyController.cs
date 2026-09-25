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
        public async Task<IActionResult> GetTrendingGifs(CancellationToken cancellationToken)
        {
            var result = await klipyService.GetTrendingGifs(cancellationToken);
            return result.Match(
                gifs => Ok(gifs),
                errors => Problem(errors)
            );
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchGifs([FromQuery] string query, CancellationToken cancellationToken)
        {
            var result = await klipyService.SearchGifs(query, cancellationToken);
            return result.Match(
                gifs => Ok(gifs),
                errors => Problem(errors)
            );
        }
    }
}

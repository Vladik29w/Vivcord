using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class MediaController(IBlobStorageService blobStorageService) : ApiMainController
    {
        [HttpPost("upload-token")]
        public IActionResult GetUploadToken([FromBody] UploadTokenRequest request)
        {
            if (CurrentUserId is null)
                return Unauthorized();

            var result = blobStorageService.GenerateUploadSasToken(BlobContainers.ChatMedia, request.FileName, request.ContentType);
            return result.Match(Ok, Problem);
        }
    }
}

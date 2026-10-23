using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ConversationController(
    IConversationQueryService queryService,
    IConversationCommandService commandService) : ApiMainController
{
    [HttpGet("{conversationId:long}")]
    public async Task<IActionResult> GetConversation(long conversationId, CancellationToken ct)
    {
        var result = await queryService.GetConversationAsync(CurrentUserId!.Value, conversationId, ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("")]
    [HttpGet("all")]
    public async Task<IActionResult> GetUserConversations(CancellationToken ct)
    {
        var result = await queryService.GetUserConversationsAsync(CurrentUserId!.Value, ct: ct);
        return result.Match(Ok, Problem);
    }

    [HttpDelete("delete/{conversationId:long}")]
    public async Task<IActionResult> DeleteConversation(long conversationId, CancellationToken ct)
    {
        var result = await commandService.DeleteForUserAsync(CurrentUserId!.Value, conversationId, ct: ct);
        return result.Match(_ => Ok(), Problem);
    }
}

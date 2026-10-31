using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers;

[Route("[controller]")]
[Authorize]
public class FriendController(IFriendService friendService) : ApiMainController
{
    [HttpGet("list")]
    public async Task<IActionResult> GetFriendList(CancellationToken ct)
    {
        var result = await friendService.GetFriendListAsync(CurrentUserId!.Value, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("send-request")]
    public async Task<IActionResult> SendFriendRequest([FromBody] AddFriendRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return Problem(Error.Validation("UsernameRequired", "Username is required."));

        var result = await friendService.SendFriendRequestAsync(CurrentUserId!.Value, request.Username, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("accept/{conversationId:long}")]
    public async Task<IActionResult> AcceptFriendRequest(long conversationId, CancellationToken ct)
    {
        var result = await friendService.AcceptFriendRequestAsync(CurrentUserId!.Value, conversationId, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("decline/{conversationId:long}")]
    public async Task<IActionResult> DeclineFriendRequest(long conversationId, CancellationToken ct)
    {
        var result = await friendService.DeclineFriendRequestAsync(CurrentUserId!.Value, conversationId, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("cancel/{conversationId:long}")]
    public async Task<IActionResult> CancelFriendRequest(long conversationId, CancellationToken ct)
    {
        var result = await friendService.CancelFriendRequestAsync(CurrentUserId!.Value, conversationId, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpDelete("remove/{username}")]
    public async Task<IActionResult> RemoveFriend([FromRoute] string username, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Problem(Error.Validation("UsernameRequired", "Username is required."));

        var result = await friendService.RemoveFriendAsync(CurrentUserId!.Value, username, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpGet("incoming-requests")]
    public async Task<IActionResult> GetIncomingRequests(CancellationToken ct)
    {
        var result = await friendService.GetIncomingRequestsAsync(CurrentUserId!.Value, ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("outgoing-requests")]
    public async Task<IActionResult> GetOutgoingRequests(CancellationToken ct)
    {
        var result = await friendService.GetOutgoingRequestsAsync(CurrentUserId!.Value, ct);
        return result.Match(Ok, Problem);
    }
}

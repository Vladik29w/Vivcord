using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers;

[Route("[controller]")]
[Authorize]
public class GroupController(IGroupService groupService) : ApiMainController
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupConversationDto dto, CancellationToken ct)
    {
        var result = await groupService.CreateGroupAsync(CurrentUserId!.Value, dto.Name, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("invite/{conversationId:long}")]
    public async Task<IActionResult> InviteMember(long conversationId, [FromBody] InviteToGroupRequest request, CancellationToken ct)
    {
        var result = await groupService.InviteMemberAsync(CurrentUserId!.Value, conversationId, request.Username, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("accept/{conversationId:long}")]
    public async Task<IActionResult> AcceptInvite(long conversationId, CancellationToken ct)
    {
        var result = await groupService.AcceptInviteAsync(CurrentUserId!.Value, conversationId, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("decline/{conversationId:long}")]
    public async Task<IActionResult> DeclineInvite(long conversationId, CancellationToken ct)
    {
        var result = await groupService.DeclineInviteAsync(CurrentUserId!.Value, conversationId, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("leave/{conversationId:long}")]
    public async Task<IActionResult> LeaveGroup(long conversationId, CancellationToken ct)
    {
        var result = await groupService.LeaveGroupAsync(CurrentUserId!.Value, conversationId, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpDelete("kick/{conversationId:long}")]
    public async Task<IActionResult> KickMember(long conversationId, [FromQuery] string username, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Problem(Error.Validation("UsernameRequired", "Username is required."));

        var result = await groupService.KickMemberAsync(CurrentUserId!.Value, conversationId, username, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("assign-admin/{conversationId:long}")]
    public async Task<IActionResult> AssignAdmin(long conversationId, [FromBody] AssignAdminRequest request, CancellationToken ct)
    {
        var result = await groupService.AssignAdminAsync(CurrentUserId!.Value, conversationId, request.NewAdminUsername, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpDelete("delete/{conversationId:long}")]
    public async Task<IActionResult> DeleteGroup(long conversationId, CancellationToken ct)
    {
        var result = await groupService.DeleteGroupAsync(CurrentUserId!.Value, conversationId, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpGet("pending-invites")]
    public async Task<IActionResult> GetPendingInvites(CancellationToken ct)
    {
        var result = await groupService.GetPendingInvitesAsync(CurrentUserId!.Value, ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("my-groups")]
    public async Task<IActionResult> GetMyGroups(CancellationToken ct)
    {
        var result = await groupService.GetUserGroupsAsync(CurrentUserId!.Value, ct);
        return result.Match(Ok, Problem);
    }
}

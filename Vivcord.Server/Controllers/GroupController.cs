using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class GroupController(IGroupChatService groupChatService) : ApiMainController
    {
        [HttpPost("create")]
        public async Task<IActionResult> CreateGroup(CreateGroupChatDTO dto, CancellationToken cancellationToken)
        {
            if (CurrentUserId is not { } userId)
                return Unauthorized();

            var result = await groupChatService.CreateGroupAsync(userId, dto, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpDelete("delete/{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId, CancellationToken cancellationToken)
        {
            if (CurrentUserId is not { } userId)
                return Unauthorized();

            var result = await groupChatService.DeleteGroupAsync(userId, groupId, cancellationToken);
            return result.Match(_ => Ok(), Problem);
        }

        [HttpPost("add-member/{groupId}")]
        public async Task<IActionResult> AddMember(int groupId, [FromBody] AddGroupMemberRequest? request, [FromQuery] string? username, CancellationToken cancellationToken)
        {
            var targetUsername = request?.Username ?? username;
            if (string.IsNullOrWhiteSpace(targetUsername))
                return Problem(Error.Validation("UsernameRequired", "Username is required."));

            if (CurrentUserId is not { } userId)
                return Unauthorized();

            var result = await groupChatService.AddMemberAsync(userId, groupId, targetUsername, cancellationToken);
            return result.Match(_ => Ok(), Problem);
        }

        [HttpDelete("remove-member/{groupId}")]
        public async Task<IActionResult> RemoveMember(int groupId, [FromBody] AddGroupMemberRequest? request, [FromQuery] string? username, CancellationToken cancellationToken)
        {
            var targetUsername = request?.Username ?? username;
            if (string.IsNullOrWhiteSpace(targetUsername))
                return Problem(Error.Validation("UsernameRequired", "Username is required."));

            if (CurrentUserId is not { } userId)
                return Unauthorized();

            var result = await groupChatService.RemoveMemberAsync(userId, groupId, targetUsername, cancellationToken);
            return result.Match(_ => Ok(), Problem);
        }

        [HttpPost("assign-admin/{groupId}")]
        public async Task<IActionResult> AssignAdmin(int groupId, [FromBody] AssignGroupAdminRequest? request, [FromQuery] string? newAdminUsername, CancellationToken cancellationToken)
        {
            var targetAdmin = request?.NewAdminUsername ?? newAdminUsername;
            if (string.IsNullOrWhiteSpace(targetAdmin))
                return Problem(Error.Validation("UsernameRequired", "New admin username is required."));

            if (CurrentUserId is not { } userId)
                return Unauthorized();

            var result = await groupChatService.AssignAdminAsync(userId, groupId, targetAdmin, cancellationToken);
            return result.Match(_ => Ok(), Problem);
        }

        [HttpGet("get/{groupId}")]
        public async Task<IActionResult> GetGroup(int groupId, CancellationToken cancellationToken)
        {
            var result = await groupChatService.GetGroupAsync(groupId, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpGet("my-groups")]
        public async Task<IActionResult> GetMyGroups(CancellationToken cancellationToken)
        {
            if (CurrentUserId is not { } userId)
                return Unauthorized();

            var result = await groupChatService.GetUserGroupsAsync(userId, cancellationToken);
            return result.Match(Ok, Problem);
        }
    }
}

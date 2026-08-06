using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var createResult = await groupChatService.CreateGroupAsync(userIdGuid, dto, cancellationToken);

            return createResult.Match(
                groupChat => Ok(groupChat),
                errors => Problem(errors)
            );
        }

        [HttpDelete("delete/{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var deleteResult = await groupChatService.DeleteGroupAsync(userIdGuid, groupId, cancellationToken);

            return deleteResult.Match(
                success => Ok(),
                errors => Problem(errors)
            );
        }

        [HttpPost("add-member/{groupId}")]
        public async Task<IActionResult> AddMember(int groupId, string username, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var addMemberResult = await groupChatService.AddMemberAsync(userIdGuid, groupId, username, cancellationToken);

            return addMemberResult.Match(
                success => Ok(),
                errors => Problem(errors)
            );
        }

        [HttpDelete("remove-member/{groupId}")]
        public async Task<IActionResult> RemoveMember(int groupId, string username, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var removeMemberResult = await groupChatService.RemoveMemberAsync(userIdGuid, groupId, username, cancellationToken);

            return removeMemberResult.Match(
                success => Ok(),
                errors => Problem(errors)
            );
        }

        [HttpPost("assign-admin/{groupId}")]
        public async Task<IActionResult> AssignAdmin(int groupId, string newAdminUsername, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var assignAdminResult = await groupChatService.AssignAdminAsync(userIdGuid, groupId, newAdminUsername, cancellationToken);

            return assignAdminResult.Match(
                success => Ok(),
                errors => Problem(errors)
            );
        }

        [HttpGet("get/{groupId}")]
        public async Task<IActionResult> GetGroup(int groupId, CancellationToken cancellationToken)
        {
            var getResult = await groupChatService.GetGroupAsync(groupId, cancellationToken);

            return getResult.Match(
                groupChat => Ok(groupChat),
                errors => Problem(errors)
            );
        }

        [HttpGet("my-groups")]
        public async Task<IActionResult> GetMyGroups(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User id claim not found.");

            if (!Guid.TryParse(userId, out var userIdGuid))
                return BadRequest("Invalid user id format.");

            var groupsResult = await groupChatService.GetUserGroupsAsync(userIdGuid, cancellationToken);

            return groupsResult.Match(
                groups => Ok(groups),
                errors => Problem(errors)
            );
        }
    }
}

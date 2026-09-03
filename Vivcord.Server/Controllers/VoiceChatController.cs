using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;

namespace Vivcord.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class VoiceChatController(IVoiceChatService voiceChatService, MainDbContext dbContext) : ApiMainController
    {
        /// <summary>
        /// Initiates a private voice call between two mutually-friended users.
        /// Validates that both caller and target have each other in their friend list,
        /// then generates a Livekit token for a new unique room.
        /// </summary>
        [HttpPost("private-call")]
        public async Task<IActionResult> InitiatePrivateCall(
            [FromBody] PrivateCallRequestDTO request,
            CancellationToken cancellationToken)
        {
            var callerId = GetCallerGuid();
            if (callerId == null) return BadRequest("User id claim not found.");

            var target = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == request.TargetUsername, cancellationToken);

            if (target == null)
                return NotFound($"User '{request.TargetUsername}' not found.");

            // Check mutual friendship
            var callerHasTarget = await dbContext.UserFriends
                .AnyAsync(uf => uf.UserId == callerId.Value && uf.FriendId == target.Id, cancellationToken);

            var targetHasCaller = await dbContext.UserFriends
                .AnyAsync(uf => uf.UserId == target.Id && uf.FriendId == callerId.Value, cancellationToken);

            if (!callerHasTarget || !targetHasCaller)
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    detail: "Voice calls are only available between mutual friends.");

            var sortedIds = new[] { callerId.Value, target.Id }.OrderBy(id => id).ToList();
            var roomId = $"voice_private_{sortedIds[0]}_{sortedIds[1]}";
            var identity = callerId.Value.ToString();
            var displayName = User.FindFirstValue("displayName");
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? identity;

            var token = voiceChatService.GenerateToken(roomId, identity, displayName);
            return Ok(new VoiceCallResponseDTO(roomId, token));
        }

        
        [HttpPost("group-call")]
        public async Task<IActionResult> InitiateGroupCall(
            [FromBody] GroupCallRequestDTO request,
            CancellationToken cancellationToken)
        {
            var callerId = GetCallerGuid();
            if (callerId == null) return BadRequest("User id claim not found.");

            var group = await dbContext.GroupChats
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.id == request.GroupId, cancellationToken);

            if (group == null)
                return NotFound("Group not found.");

            var isMember = await dbContext.GroupChatMembers
                .AnyAsync(gcm => gcm.GroupChatId == request.GroupId && gcm.UserId == callerId.Value, cancellationToken);

            if (!isMember)
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    detail: "You are not a member of this group.");

            if (group.VoiceRoomId == Guid.Empty)
            {
                var groupToUpdate = await dbContext.GroupChats.FirstOrDefaultAsync(g => g.id == request.GroupId, cancellationToken);
                if (groupToUpdate != null && groupToUpdate.VoiceRoomId == Guid.Empty)
                {
                    groupToUpdate.VoiceRoomId = Guid.NewGuid();
                    await dbContext.SaveChangesAsync(cancellationToken);
                    group = groupToUpdate;
                }
            }

            var roomId = group.VoiceRoomId.ToString();
            var identity = callerId.Value.ToString();
            var displayName = User.FindFirstValue("displayName");
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? identity;

            var token = voiceChatService.GenerateToken(roomId, identity, displayName);
            return Ok(new VoiceCallResponseDTO(roomId, token));
        }

        private Guid? GetCallerGuid()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId);

            if (string.IsNullOrEmpty(userId)) return null;
            if (!Guid.TryParse(userId, out var guid)) return null;
            return guid;
        }
    }
}

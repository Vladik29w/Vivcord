using ErrorOr;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services
{
    public interface IVoiceChatService
    {
        string GenerateToken(string roomId, string identity, string displayName, string? metadata = null);
        Task<ErrorOr<VoiceCallResponseDTO>> InitiatePrivateCallAsync(Guid callerId, string? callerDisplayName, string targetUsername, CancellationToken cancellationToken = default);
        Task<ErrorOr<VoiceCallResponseDTO>> InitiateGroupCallAsync(Guid callerId, string? callerDisplayName, int groupId, CancellationToken cancellationToken = default);
    }

    public class VoiceChatService(IConfiguration config, MainDbContext dbContext) : IVoiceChatService
    {
        private readonly string apiKey = config["Livekit:ApiKey"] ?? throw new InvalidOperationException("Livekit API Key is not configured.");
        private readonly string apiSecret = config["Livekit:ApiSecret"] ?? throw new InvalidOperationException("Livekit API Secret is not configured.");

        public string GenerateToken(string roomId, string identity, string displayName, string? metadata = null)
        {
            var token = new AccessToken(apiKey, apiSecret)
                .WithIdentity(identity)
                .WithName(displayName)
                .WithMetadata(metadata ?? string.Empty)
                .WithTtl(TimeSpan.FromMinutes(30))
                .WithGrants(new VideoGrants
                {
                    RoomJoin = true,
                    Room = roomId,
                    CanPublish = true,
                    CanSubscribe = true,
                    CanPublishData = true
                });
            return token.ToJwt();
        }

        public async Task<ErrorOr<VoiceCallResponseDTO>> InitiatePrivateCallAsync(
            Guid callerId,
            string? callerDisplayName,
            string targetUsername,
            CancellationToken cancellationToken = default)
        {
            var target = await dbContext.Users
                .Where(u => u.UserName == targetUsername)
                .Select(u => new { u.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (target == null)
                return Error.NotFound("UserNotFound", $"User '{targetUsername}' not found.");

            if (target.Id == callerId)
                return Error.Conflict("InvalidCall", "You cannot call yourself.");

            var areMutualFriends = await dbContext.UserFriends
                .CountAsync(uf => (uf.UserId == callerId && uf.FriendId == target.Id) ||
                                  (uf.UserId == target.Id && uf.FriendId == callerId), cancellationToken) == 2;

            if (!areMutualFriends)
                return Error.Forbidden("NotFriends", "Voice calls are only available between mutual friends.");

            var sortedIds = new[] { callerId, target.Id }.OrderBy(id => id).ToList();
            var roomId = $"voice_private_{sortedIds[0]}_{sortedIds[1]}";
            var identity = callerId.ToString();
            var displayName = string.IsNullOrWhiteSpace(callerDisplayName) ? identity : callerDisplayName;

            var profilePictureUrl = await dbContext.Users
                .Where(u => u.Id == callerId)
                .Select(u => u.ProfilePictureUrl)
                .FirstOrDefaultAsync(cancellationToken);

            var token = GenerateToken(roomId, identity, displayName, profilePictureUrl);
            return new VoiceCallResponseDTO(roomId, token);
        }

        public async Task<ErrorOr<VoiceCallResponseDTO>> InitiateGroupCallAsync(
            Guid callerId,
            string? callerDisplayName,
            int groupId,
            CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats
                .FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);

            if (group == null)
                return Error.NotFound("GroupNotFound", "Group not found.");

            var isMember = await dbContext.GroupChatMembers
                .AnyAsync(gcm => gcm.GroupChatId == groupId && gcm.UserId == callerId, cancellationToken);

            if (!isMember)
                return Error.Forbidden("NotMember", "You are not a member of this group.");

            if (group.VoiceRoomId == Guid.Empty)
            {
                group.VoiceRoomId = Guid.NewGuid();
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var roomId = group.VoiceRoomId.ToString();
            var identity = callerId.ToString();
            var displayName = string.IsNullOrWhiteSpace(callerDisplayName) ? identity : callerDisplayName;

            var profilePictureUrl = await dbContext.Users
                .Where(u => u.Id == callerId)
                .Select(u => u.ProfilePictureUrl)
                .FirstOrDefaultAsync(cancellationToken);

            var token = GenerateToken(roomId, identity, displayName, profilePictureUrl);
            return new VoiceCallResponseDTO(roomId, token);
        }
    }
}

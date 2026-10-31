using ErrorOr;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services
{
    public interface IVoiceChatService
    {
        string GenerateToken(string roomId, string identity, string displayName, string? metadata = null);
        Task<ErrorOr<VoiceCallResponseDTO>> InitiateVoiceCall(Guid callerId, string? callerDisplayName, long conversationId, CancellationToken cancellationToken = default);
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

        public async Task<ErrorOr<VoiceCallResponseDTO>> InitiateVoiceCall(
            Guid callerId,
            string? callerDisplayName,
            long conversationId,
            CancellationToken cancellationToken = default)
        {
            var conversation = await dbContext.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId)
                .Select(c => new
                {
                    c.Id,
                    c.Type,
                    CallerMember = c.Members
                        .Where(m => m.UserId == callerId)
                        .Select(m => new
                        {
                            AvatarUrl = m.User.ProfilePictureUrl,
                            DisplayName = !string.IsNullOrWhiteSpace(m.User.DisplayName)
                                ? m.User.DisplayName
                                : m.User.UserName
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (conversation == null)
                return Error.NotFound("Conversation.NotFound", $"Conversation with ID {conversationId} was not found.");

            if (conversation.CallerMember == null)
            {
                if (conversation.Type == ConversationType.Group)
                    return Error.Forbidden("Conversation.Forbidden", "You are not a member of this group.");

                return Error.Forbidden("Conversation.Forbidden", "You are not a participant in this conversation.");
            }

            var roomId = conversationId.ToString();
            var identity = callerId.ToString();
            var displayName = !string.IsNullOrWhiteSpace(callerDisplayName)
                ? callerDisplayName
                : (!string.IsNullOrWhiteSpace(conversation.CallerMember.DisplayName)
                    ? conversation.CallerMember.DisplayName
                    : identity);

            var token = GenerateToken(roomId, identity, displayName, conversation.CallerMember.AvatarUrl);
            return new VoiceCallResponseDTO(roomId, token);
        }
    }
}

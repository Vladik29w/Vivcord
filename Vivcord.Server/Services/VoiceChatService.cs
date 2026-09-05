using Livekit.Server.Sdk.Dotnet;

namespace Vivcord.Server.Services
{
    public interface IVoiceChatService
    {
        string GenerateToken(string roomId, string identity, string displayName, string? metadata = null);
    }
    public class VoiceChatService(IConfiguration config) : IVoiceChatService
    {
        string apiKey = config["Livekit:ApiKey"] ?? throw new InvalidOperationException("Livekit API Key is not configured.");
        string apiSecret = config["Livekit:ApiSecret"] ?? throw new InvalidOperationException("Livekit API Secret is not configured.");

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
    }
}

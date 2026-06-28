using Livekit.Server.Sdk.Dotnet;

namespace Vivcord.Server.Services
{
    public interface IVoiceChatService
    {
        string GenerateToken(string roomName, string identity);
    }
    public class VoiceChatService(IConfiguration config) : IVoiceChatService
    {
        string apiKey = config["Livekit:ApiKey"] ?? throw new InvalidOperationException("Livekit API Key is not configured.");
        string apiSecret = config["Livekit:ApiSecret"] ?? throw new InvalidOperationException("Livekit API Secret is not configured.");

        public string GenerateToken(string roomName, string identity)
        {
            var token = new AccessToken(apiKey, apiSecret)
                .WithIdentity(identity)
                .WithName(identity)
                .WithTtl(TimeSpan.FromMinutes(30))
                .WithGrants(new VideoGrants
                {
                    RoomJoin = true,
                    Room = roomName,
                    CanPublish = true,
                    CanSubscribe = true,
                    CanPublishData = true
                });
            return token.ToJwt();
        }
    }
}

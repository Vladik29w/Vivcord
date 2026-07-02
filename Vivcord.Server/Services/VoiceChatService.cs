using Livekit.Server.Sdk.Dotnet;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services
{
    public interface IVoiceChatService
    {
        string GenerateToken(VoiceTokenDTO voiceToken);
    }
    public class VoiceChatService(IConfiguration config) : IVoiceChatService
    {
        string apiKey = config["Livekit:ApiKey"] ?? throw new InvalidOperationException("Livekit API Key is not configured.");
        string apiSecret = config["Livekit:ApiSecret"] ?? throw new InvalidOperationException("Livekit API Secret is not configured.");

        public string GenerateToken(VoiceTokenDTO voiceToken)
        {
            var token = new AccessToken(apiKey, apiSecret)
                .WithIdentity(voiceToken.Identity)
                .WithName(voiceToken.DisplayName)
                .WithTtl(TimeSpan.FromMinutes(30))
                .WithGrants(new VideoGrants
                {
                    RoomJoin = true,
                    Room = voiceToken.RoomName,
                    CanPublish = true,
                    CanSubscribe = true,
                    CanPublishData = true
                });
            return token.ToJwt();
        }
    }
}

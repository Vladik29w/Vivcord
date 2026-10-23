using System.Text.Json.Serialization;

namespace Vivcord.Server.DTO
{
    public record VoiceCallRequestDTO(
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        long ConversationId
    );

    public record PrivateCallRequestDTO(string TargetUsername);

    public record GroupCallRequestDTO(int GroupId);

    public record VoiceCallResponseDTO(string RoomId, string Token);
}

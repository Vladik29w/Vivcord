namespace Vivcord.Server.DTO
{
    public record PrivateCallRequestDTO(string TargetUsername);
    public record GroupCallRequestDTO(int GroupId);
    public record VoiceCallResponseDTO(string RoomId, string Token);
}

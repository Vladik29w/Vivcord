namespace Vivcord.Server.DTO
{
    public record SendPrivateMessageDto(string TargetUserId, string Text, string? AttachmentUrl, string? AttachmentType);
}

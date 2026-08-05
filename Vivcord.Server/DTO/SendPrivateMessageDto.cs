namespace Vivcord.Server.DTO
{
    public record SendPrivateMessageDto(Guid TargetUserId, string Text, string? AttachmentUrl, string? AttachmentType);
}

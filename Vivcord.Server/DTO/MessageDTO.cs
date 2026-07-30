namespace Vivcord.Server.DTO
{
    public record MessageDto(int Id, string SenderId, string Text, string? AttachmentUrl, string? AttachmentType);
}


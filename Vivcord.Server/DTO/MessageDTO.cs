namespace Vivcord.Server.DTO
{
    public record MessageDto
    {
        public int Id { get; set; }
        public Guid SenderId { get; set; }
        public Guid TargetUserId { get; set; }
        public string Text { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; }
    }
}

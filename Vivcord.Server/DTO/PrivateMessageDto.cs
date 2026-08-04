namespace Vivcord.Server.DTO
{
    public record PrivateMessageDto
    {
        public int Id { get; set; }
        public Guid SenderId { get; set; }
        public Guid TargetUserId { get; set; }
        public string Text { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; }
    }
    public record GroupMessageDto
    {
        public int Id { get; set; }
        public Guid SenderId { get; set; }
        public string? SenderName { get; set; }
        public int GroupId { get; set; }
        public string Text { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; }
    }
}

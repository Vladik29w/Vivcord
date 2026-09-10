namespace Vivcord.Server.DTO
{
    public record PrivateMessageDto
    {
        public int Id { get; init; }
        public Guid SenderId { get; init; }
        public Guid TargetUserId { get; init; }
        public required string Text { get; init; }
        public string? AttachmentUrl { get; init; }
        public string? AttachmentType { get; init; }
        public DateTimeOffset SentAt { get; init; }
        public string? SenderName { get; init; }
        public string? SenderAvatarUrl { get; init; }
    }

    public record GroupMessageDto
    {
        public int Id { get; init; }
        public Guid SenderId { get; init; }
        public string? SenderName { get; init; }
        public string? SenderAvatarUrl { get; init; }
        public int GroupId { get; init; }
        public required string Text { get; init; }
        public string? AttachmentUrl { get; init; }
        public string? AttachmentType { get; init; }
        public DateTimeOffset SentAt { get; init; }
    }

    public record SendPrivateMessageDto(
        Guid TargetUserId,
        string Text,
        string? AttachmentUrl = null,
        string? AttachmentType = null
    );

    public record SendGroupMessageDto(
        int GroupId,
        string Text,
        string? AttachmentUrl = null,
        string? AttachmentType = null
    );

    public record MessageSendResult(
        int Id,
        string? SasAttachmentUrl,
        DateTimeOffset SentAt = default,
        string? SenderName = null,
        string? SenderAvatarUrl = null
    );

    public record MessageBroadcastDto(
        string SenderId,
        string Text,
        int MessageId,
        string? AttachmentUrl,
        string? AttachmentType,
        string? SenderName,
        string? SenderAvatarUrl,
        DateTimeOffset SentAt
    );

    public record GetChatHistoryQuery(
        int Limit = 50,
        DateTimeOffset? Before = null,
        int? BeforeId = null
    );
}

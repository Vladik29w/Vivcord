using System.Text.Json.Serialization;

namespace Vivcord.Server.DTO
{
    public record MessageDto
    {
        [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        public long Id { get; init; }

        [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        public long ConversationId { get; init; }

        public Guid SenderId { get; init; }
        public string? SenderName { get; init; }
        public string? SenderAvatarUrl { get; init; }
        public required string Text { get; init; }
        public string? AttachmentUrl { get; init; }
        public string? AttachmentType { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? EditedAt { get; init; }

        // Convenience / backward compatibility aliases
        public DateTimeOffset SentAt => CreatedAt;
        public DateTimeOffset Timestamp => CreatedAt;

        [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        public long? GroupId => ConversationId;
    }

    public record SendMessageDto(
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        long ConversationId,
        string Text,
        string? AttachmentUrl = null,
        string? AttachmentType = null
    );

    public record SendMessageRequestDto(
        long ConversationId,
        Guid SenderId,
        string? SenderDisplayName,
        string Text,
        string? AttachmentUrl = null,
        string? AttachmentType = null
    );

    public record MessageSendResult(
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        long Id,
        string? SasAttachmentUrl,
        DateTimeOffset CreatedAt = default,
        string? SenderName = null,
        string? SenderAvatarUrl = null
    )
    {
        public DateTimeOffset SentAt => CreatedAt;
    }

    public record MessageBroadcastDto(
        string SenderId,
        string Text,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        long MessageId,
        string? AttachmentUrl,
        string? AttachmentType,
        string? SenderName,
        string? SenderAvatarUrl,
        DateTimeOffset SentAt,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        long ConversationId
    );

    public record GetChatHistoryQuery(
        int Limit = 50,
        DateTimeOffset? Before = null,
        long? BeforeId = null
    );
}

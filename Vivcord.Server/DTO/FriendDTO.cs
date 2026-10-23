using System.Text.Json.Serialization;

namespace Vivcord.Server.DTO
{
    public record FriendDTO(
        Guid Id,
        string UserName,
        string? DisplayName = null,
        string? ProfilePictureUrl = null,
        bool IsOnline = false,
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        long? ConversationId = null
    );

    public record AddFriendRequest(string Username);

    public record PendingFriendRequestDto(
        [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        long ConversationId,
        Guid UserId,
        string UserName,
        string? DisplayName,
        string? ProfilePictureUrl,
        DateTimeOffset CreatedAt
    );
}

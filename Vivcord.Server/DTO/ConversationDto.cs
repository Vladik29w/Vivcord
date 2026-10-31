using System.Text.Json.Serialization;
using Vivcord.Server.Models;

namespace Vivcord.Server.DTO;

public record ConversationDto
{
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public long Id { get; init; }
    public string Type { get; init; } = string.Empty; // "Direct" | "Group"
    public string? Title { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastMessageAt { get; init; }
    public IReadOnlyList<ConversationMemberDto> Members { get; init; } = [];
}

public record ConversationMemberDto(
    Guid UserId,
    string UserName,
    string DisplayName,
    string? ProfilePictureUrl,
    string Role,
    string Status, // "Pending" | "Joined" | "Left"
    Guid? InvitedById = null
);

public record CreateConversationReqDto(
    ConversationType Type,
    string? Title = null,
    string? DirectPairKey = null,
    IReadOnlyList<ConversationMember>? InitialMembers = null
);

public record AddMemberReqDto(
    long ConversationId,
    Guid UserId,
    RoleType Role = RoleType.Member,
    MemberStatus Status = MemberStatus.Pending,
    Guid? InvitedById = null
);

public record CreateGroupConversationDto(string Name);
public record InviteToGroupRequest(string Username);
public record AssignAdminRequest(string NewAdminUsername);

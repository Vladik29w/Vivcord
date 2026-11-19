using Riok.Mapperly.Abstractions;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Mappers;

[Mapper]
public partial class ConversationMapperly
{
    public static ConversationMapperly Instance { get; } = new();

    [MapProperty(nameof(Conversation.Type), nameof(ConversationDto.Type), Use = nameof(MapType))]
    [MapProperty(nameof(Conversation.Members), nameof(ConversationDto.Members), Use = nameof(MapMembers))]
    public partial ConversationDto ToDto(Conversation conversation);

    private static string MapType(ConversationType type) => type.ToString();

    private static IReadOnlyList<ConversationMemberDto> MapMembers(ICollection<ConversationMember> members) =>
        members.Select(MapMember).ToList();

    private static ConversationMemberDto MapMember(ConversationMember member)
    {
        var userName = member.User?.UserName ?? string.Empty;
        var displayName = member.User?.DisplayName;

        return new ConversationMemberDto(
            member.UserId,
            userName,
            string.IsNullOrWhiteSpace(displayName) ? userName : displayName,
            member.User?.ProfilePictureUrl,
            member.Role.ToString(),
            member.Status.ToString(),
            member.InvitedById
        );
    }
}

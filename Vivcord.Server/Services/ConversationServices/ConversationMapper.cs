using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services;

public static class ConversationMapper
{
    public static ConversationDto ToDto(this Conversation conversation)
    {
        return new ConversationDto
        {
            Id = conversation.Id,
            Type = conversation.Type.ToString(),
            Title = conversation.Title,
            AvatarUrl = conversation.AvatarUrl,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            Members = conversation.Members.Select(m => new ConversationMemberDto(
                m.UserId,
                m.User?.UserName ?? string.Empty,
                !string.IsNullOrWhiteSpace(m.User?.DisplayName) ? m.User.DisplayName : (m.User?.UserName ?? string.Empty),
                m.User?.ProfilePictureUrl,
                m.Role.ToString(),
                m.Status.ToString(),
                m.InvitedById
            )).ToList()
        };
    }
}

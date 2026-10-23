using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services;

public interface IFriendService
{
    Task<ErrorOr<IReadOnlyList<FriendDTO>>> GetFriendListAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<ConversationDto>> SendFriendRequestAsync(Guid userId, string targetUsername, CancellationToken ct = default);
    Task<ErrorOr<Success>> AcceptFriendRequestAsync(Guid userId, long conversationId, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeclineFriendRequestAsync(Guid userId, long conversationId, CancellationToken ct = default);
    Task<ErrorOr<Success>> CancelFriendRequestAsync(Guid userId, long conversationId, CancellationToken ct = default);
    Task<ErrorOr<Success>> RemoveFriendAsync(Guid userId, string friendUsername, CancellationToken ct = default);
    Task<ErrorOr<IReadOnlyList<PendingFriendRequestDto>>> GetIncomingRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<IReadOnlyList<PendingFriendRequestDto>>> GetOutgoingRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetFriendIdsAsync(Guid userId, CancellationToken ct = default);
}

public class FriendService(
    MainDbContext dbContext,
    IConversationCommandService commandService,
    IUserStatusService? userStatusService = null) : IFriendService
{
    public async Task<ErrorOr<IReadOnlyList<FriendDTO>>> GetFriendListAsync(Guid userId, CancellationToken ct = default)
    {
        // Direct conversations where BOTH members are Joined
        var directConversations = await dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.Type == ConversationType.Direct &&
                        c.Members.Count == 2 &&
                        c.Members.All(m => m.Status == MemberStatus.Joined) &&
                        c.Members.Any(m => m.UserId == userId))
            .Include(c => c.Members).ThenInclude(m => m.User)
            .ToListAsync(ct);

        var friends = directConversations.Select(c =>
        {
            var friendMember = c.Members.First(m => m.UserId != userId);
            var friendUser = friendMember.User;
            return new FriendDTO(
                friendUser.Id,
                friendUser.UserName!,
                !string.IsNullOrWhiteSpace(friendUser.DisplayName) ? friendUser.DisplayName : friendUser.UserName,
                friendUser.ProfilePictureUrl,
                userStatusService?.IsUserActive(friendUser.Id) ?? false,
                c.Id
            );
        }).ToList();

        return friends;
    }

    public async Task<ErrorOr<ConversationDto>> SendFriendRequestAsync(Guid userId, string targetUsername, CancellationToken ct = default)
    {
        var targetUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == targetUsername, ct);
        if (targetUser is null)
            return Error.NotFound("User.NotFound", $"User '{targetUsername}' not found.");

        if (userId == targetUser.Id)
            return Error.Conflict("Friend.Self", "You cannot send a friend request to yourself.");

        var pairKey = ConversationCommandService.GenerateDirectPairKey(userId, targetUser.Id);

        var existingConv = await dbContext.Conversations
            .Include(c => c.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.DirectPairKey == pairKey, ct);

        if (existingConv is not null)
        {
            var senderMember = existingConv.Members.FirstOrDefault(m => m.UserId == userId);
            var targetMember = existingConv.Members.FirstOrDefault(m => m.UserId == targetUser.Id);

            if (senderMember is not null && targetMember is not null)
            {
                if (senderMember.Status is MemberStatus.Joined && targetMember.Status is MemberStatus.Joined)
                    return Error.Conflict("Friend.AlreadyFriends", "You are already friends.");

                if (senderMember.Status is MemberStatus.Pending && targetMember.Status is MemberStatus.Joined)
                {
                    // Mutual request: target already invited us previously -> auto-accept
                    senderMember.Status = MemberStatus.Joined;
                    senderMember.JoinedAt = DateTimeOffset.UtcNow;
                    await dbContext.SaveChangesAsync(ct);
                    return existingConv.ToDto();
                }

                if (senderMember.Status is MemberStatus.Joined && targetMember.Status is MemberStatus.Pending)
                    return Error.Conflict("Friend.AlreadyPending", "Friend request is already pending.");

                // If one of the members previously left: re-activate friendship request
                senderMember.Status = MemberStatus.Joined;
                senderMember.JoinedAt = DateTimeOffset.UtcNow;
                targetMember.Status = MemberStatus.Pending;
                targetMember.InvitedById = userId;
                targetMember.JoinedAt = null;

                await dbContext.SaveChangesAsync(ct);
                return existingConv.ToDto();
            }
        }

        var request = new CreateConversationReqDto(
            Type: ConversationType.Direct,
            Title: null,
            DirectPairKey: pairKey,
            InitialMembers:
            [
                new ConversationMember
                {
                    UserId = userId,
                    Role = RoleType.Member,
                    Status = MemberStatus.Joined,
                    JoinedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new ConversationMember
                {
                    UserId = targetUser.Id,
                    Role = RoleType.Member,
                    Status = MemberStatus.Pending,
                    InvitedById = userId,
                    JoinedAt = null,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        );

        return await commandService.CreateConversationAsync(request, ct);
    }

    public async Task<ErrorOr<Success>> AcceptFriendRequestAsync(Guid userId, long conversationId, CancellationToken ct = default)
    {
        return await commandService.JoinConversationAsync(userId, conversationId, ConversationType.Direct, ct);
    }

    public async Task<ErrorOr<Success>> DeclineFriendRequestAsync(Guid userId, long conversationId, CancellationToken ct = default)
    {
        return await commandService.RemoveFromConversationAsync(userId, conversationId, ConversationType.Direct, ct);
    }

    public async Task<ErrorOr<Success>> CancelFriendRequestAsync(Guid userId, long conversationId, CancellationToken ct = default)
    {
        return await commandService.RemoveFromConversationAsync(userId, conversationId, ConversationType.Direct, ct);
    }

    public async Task<ErrorOr<Success>> RemoveFriendAsync(Guid userId, string friendUsername, CancellationToken ct = default)
    {
        var friendUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == friendUsername, ct);
        if (friendUser is null)
            return Error.NotFound("User.NotFound", $"User '{friendUsername}' not found.");

        var pairKey = ConversationCommandService.GenerateDirectPairKey(userId, friendUser.Id);
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.DirectPairKey == pairKey, ct);

        if (conversation is null)
            return Error.NotFound("Friend.NotFound", "User not found or not in your friend list.");

        return await commandService.RemoveFromConversationAsync(userId, conversation.Id, ConversationType.Direct, ct);
    }

    public async Task<ErrorOr<IReadOnlyList<PendingFriendRequestDto>>> GetIncomingRequestsAsync(Guid userId, CancellationToken ct = default)
    {
        var conversations = await dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.Type == ConversationType.Direct &&
                        c.Members.Any(m => m.UserId == userId && m.Status == MemberStatus.Pending))
            .Include(c => c.Members).ThenInclude(m => m.User)
            .ToListAsync(ct);

        var dtos = conversations.Select(c =>
        {
            var senderMember = c.Members.First(m => m.UserId != userId);
            var currentMember = c.Members.First(m => m.UserId == userId);
            var sender = senderMember.User;
            return new PendingFriendRequestDto(
                c.Id,
                sender.Id,
                sender.UserName!,
                sender.DisplayName,
                sender.ProfilePictureUrl,
                currentMember.CreatedAt
            );
        }).ToList();

        return dtos;
    }

    public async Task<ErrorOr<IReadOnlyList<PendingFriendRequestDto>>> GetOutgoingRequestsAsync(Guid userId, CancellationToken ct = default)
    {
        var conversations = await dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.Type == ConversationType.Direct &&
                        c.Members.Any(m => m.UserId == userId && m.Status == MemberStatus.Joined) &&
                        c.Members.Any(m => m.UserId != userId && m.Status == MemberStatus.Pending && m.InvitedById == userId))
            .Include(c => c.Members).ThenInclude(m => m.User)
            .ToListAsync(ct);

        var dtos = conversations.Select(c =>
        {
            var targetMember = c.Members.First(m => m.UserId != userId);
            var target = targetMember.User;
            return new PendingFriendRequestDto(
                c.Id,
                target.Id,
                target.UserName!,
                target.DisplayName,
                target.ProfilePictureUrl,
                c.CreatedAt
            );
        }).ToList();

        return dtos;
    }

    public async Task<IReadOnlyList<Guid>> GetFriendIdsAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.ConversationMembers
            .AsNoTracking()
            .Where(cm => cm.UserId == userId && cm.Status == MemberStatus.Joined && cm.Conversation.Type == ConversationType.Direct)
            .SelectMany(cm => cm.Conversation.Members)
            .Where(m => m.UserId != userId && m.Status == MemberStatus.Joined)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(ct);
    }
}

using ErrorOr;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Hubs;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services;

public interface IGroupService//CQRS separation maybe?
{
    Task<ErrorOr<ConversationDto>> CreateGroupAsync(Guid userId, string name, CancellationToken ct = default);
    Task<ErrorOr<Success>> InviteMemberAsync(Guid adminId, long conversationId, string username, CancellationToken ct = default);
    Task<ErrorOr<Success>> AcceptInviteAsync(Guid userId, long conversationId, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeclineInviteAsync(Guid userId, long conversationId, CancellationToken ct = default);
    Task<ErrorOr<Success>> LeaveGroupAsync(Guid userId, long conversationId, CancellationToken ct = default);
    Task<ErrorOr<Success>> KickMemberAsync(Guid adminId, long conversationId, string username, CancellationToken ct = default);
    Task<ErrorOr<Success>> AssignAdminAsync(Guid adminId, long conversationId, string newAdminUsername, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteGroupAsync(Guid adminId, long conversationId, CancellationToken ct = default);
    Task<ErrorOr<IReadOnlyList<ConversationDto>>> GetPendingInvitesAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<IReadOnlyList<ConversationDto>>> GetUserGroupsAsync(Guid userId, CancellationToken ct = default);
}

public class GroupService(
    MainDbContext dbContext,
    IConversationCommandService commandService,
    IConversationQueryService queryService,
    IHubContext<ChatHub>? hubContext = null) : IGroupService
{
    private async Task<List<Guid>> GetConversationMemberIdsAsync(long conversationId, CancellationToken ct)
    {
        return await dbContext.ConversationMembers
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task NotifyGroupsUpdatedAsync(IEnumerable<Guid> userIds, long conversationId, CancellationToken ct = default)
    {
        if (hubContext is null) return;
        var ids = userIds.Select(id => id.ToString()).Distinct().ToList();
        if (ids.Count > 0)
        {
            await hubContext.Clients.Users(ids).SendAsync("GroupsUpdated", conversationId.ToString(), ct);
        }
    }

    private ErrorOr<Success> ValidationChecks(Conversation conversation, Guid adminId, string username, AppUser? targetUser, CancellationToken ct = default)
    {
        if (conversation is null)
            return Error.NotFound("Group.NotFound", "Group not found.");

        if (conversation.Type != ConversationType.Group)
            return Error.Validation("Group.InvalidType", "Cannot add members to a direct conversation via this method.");

        var adminMember = conversation.Members.FirstOrDefault(m => m.UserId == adminId && m.Role == RoleType.Admin && m.Status == MemberStatus.Joined);
        if (adminMember is null)
            return Error.Unauthorized("Group.Unauthorized", "Only group admin can invite members.");

        if (targetUser is null)
            return Error.NotFound("User.NotFound", $"User '{username}' not found.");

        return Result.Success;
    }

    public async Task<ErrorOr<ConversationDto>> CreateGroupAsync(Guid userId, string name, CancellationToken ct = default)
    {
        var creator = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (creator is null)
            return Error.NotFound("User.NotFound", "User not found.");

        var request = new CreateConversationReqDto(
            Type: ConversationType.Group,
            Title: name,
            DirectPairKey: null,
            InitialMembers:
            [
                new ConversationMember
                {
                    UserId = userId,
                    User = creator,
                    Role = RoleType.Admin,
                    Status = MemberStatus.Joined,
                    JoinedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        );

        return await commandService.CreateConversationAsync(request, ct);
    }

    public async Task<ErrorOr<Success>> InviteMemberAsync(Guid adminId, long conversationId, string username, CancellationToken ct = default)
    {
        var conversation = await dbContext.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        var targetUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == username, ct);
        if (targetUser is null)
            return Error.NotFound("User.NotFound", $"User '{username}' not found.");

        var validationResult = ValidationChecks(conversation!, adminId, username, targetUser, ct);
        if (validationResult.IsError)
            return validationResult;

        var existingMember = conversation!.Members.FirstOrDefault(m => m.UserId == targetUser.Id);
        if (existingMember is not null)
        {
            if (existingMember.Status == MemberStatus.Joined)
                return Error.Conflict("Group.AlreadyMember", "User is already a member of this group.");

            if (existingMember.Status == MemberStatus.Pending)
                return Error.Conflict("Group.AlreadyPending", "User has already been invited.");
        }

        var request = new AddMemberReqDto(
            ConversationId: conversationId,
            UserId: targetUser.Id,
            Role: RoleType.Member,
            Status: MemberStatus.Pending,
            InvitedById: adminId
        );

        var result = await commandService.AddMemberAsync(request, ct);
        if (!result.IsError)
        {
            var memberIds = conversation.Members.Select(m => m.UserId).Append(targetUser.Id);
            await NotifyGroupsUpdatedAsync(memberIds, conversationId, ct);
        }

        return result;
    }

    public async Task<ErrorOr<Success>> AcceptInviteAsync(Guid userId, long conversationId, CancellationToken ct = default)
    {
        var memberIds = await GetConversationMemberIdsAsync(conversationId, ct);
        var result = await commandService.JoinConversationAsync(userId, conversationId, ConversationType.Group, ct);
        if (!result.IsError)
            await NotifyGroupsUpdatedAsync(memberIds, conversationId, ct);

        return result;
    }

    public async Task<ErrorOr<Success>> DeclineInviteAsync(Guid userId, long conversationId, CancellationToken ct = default)
    {
        var memberIds = await GetConversationMemberIdsAsync(conversationId, ct);
        var result = await commandService.RemoveFromConversationAsync(userId, conversationId, ConversationType.Group, ct);
        if (!result.IsError)
            await NotifyGroupsUpdatedAsync(memberIds, conversationId, ct);

        return result;
    }

    public async Task<ErrorOr<Success>> LeaveGroupAsync(Guid userId, long conversationId, CancellationToken ct = default)
    {
        var memberIds = await GetConversationMemberIdsAsync(conversationId, ct);
        var result = await commandService.RemoveFromConversationAsync(userId, conversationId, ConversationType.Group, ct);
        if (!result.IsError)
            await NotifyGroupsUpdatedAsync(memberIds, conversationId, ct);

        return result;
    }

    public async Task<ErrorOr<Success>> KickMemberAsync(Guid adminId, long conversationId, string username, CancellationToken ct = default)
    {
        var conversation = await dbContext.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        var targetUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == username, ct);

        var validationResult = ValidationChecks(conversation!, adminId, username, targetUser, ct);
        if (validationResult.IsError)
            return validationResult;

        if (adminId == targetUser.Id)
            return Error.Conflict("Group.SelfKick", "Admin cannot kick themselves (use LeaveGroup instead).");

        var targetMember = conversation.Members.FirstOrDefault(m => m.UserId == targetUser.Id);
        if (targetMember is null)
            return Error.NotFound("Group.NotMember", "User is not a member of the group.");

        var memberIds = conversation.Members.Select(m => m.UserId).ToList();
        var result = await commandService.RemoveFromConversationAsync(targetUser.Id, conversationId, ConversationType.Group, ct);
        if (!result.IsError)
            await NotifyGroupsUpdatedAsync(memberIds, conversationId, ct);

        return result;
    }

    public async Task<ErrorOr<Success>> AssignAdminAsync(Guid adminId, long conversationId, string newAdminUsername, CancellationToken ct = default)
    {
        var conversation = await dbContext.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return Error.NotFound("Group.NotFound", "Group not found.");

        if (conversation.Type != ConversationType.Group)
            return Error.Validation("Group.InvalidType", "Assigning admin is only available for group conversations.");

        var currentAdmin = conversation.Members.FirstOrDefault(m => m.UserId == adminId && m.Role == RoleType.Admin && m.Status == MemberStatus.Joined);
        if (currentAdmin is null)
            return Error.Unauthorized("Group.Unauthorized", "Only current group admin can assign a new admin.");

        var targetUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == newAdminUsername, ct);
        if (targetUser is null)
            return Error.NotFound("User.NotFound", $"User '{newAdminUsername}' not found.");

        var targetMember = conversation.Members.FirstOrDefault(m => m.UserId == targetUser.Id && m.Status == MemberStatus.Joined);
        if (targetMember is null)
            return Error.Conflict("Group.TargetNotMember", "User must be an active member of the group.");

        var memberIds = conversation.Members.Select(m => m.UserId).ToList();
        var result = await commandService.TransferAdminAsync(conversationId, adminId, targetUser.Id, ct);
        if (!result.IsError)
            await NotifyGroupsUpdatedAsync(memberIds, conversationId, ct);

        return result;
    }

    public async Task<ErrorOr<Success>> DeleteGroupAsync(Guid adminId, long conversationId, CancellationToken ct = default)
    {
        var memberIds = await GetConversationMemberIdsAsync(conversationId, ct);
        var result = await commandService.DeleteForUserAsync(adminId, conversationId, ConversationType.Group, ct);
        if (!result.IsError)
            await NotifyGroupsUpdatedAsync(memberIds, conversationId, ct);

        return result;
    }

    public async Task<ErrorOr<IReadOnlyList<ConversationDto>>> GetPendingInvitesAsync(Guid userId, CancellationToken ct = default)
    {
        return await queryService.GetPendingConversationsAsync(userId, ConversationType.Group, ct);
    }

    public async Task<ErrorOr<IReadOnlyList<ConversationDto>>> GetUserGroupsAsync(Guid userId, CancellationToken ct = default)
    {
        return await queryService.GetUserConversationsAsync(userId, ConversationType.Group, ct);
    }
}

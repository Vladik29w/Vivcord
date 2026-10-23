using System.Data;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services;

public interface IConversationCommandService
{
    Task<ErrorOr<ConversationDto>> CreateConversationAsync(CreateConversationReqDto createQequest, CancellationToken ct = default);
    Task<ErrorOr<Success>> JoinConversationAsync(Guid userId, long conversationId, ConversationType? expectedType = null, CancellationToken ct = default);
    Task<ErrorOr<Success>> RemoveFromConversationAsync(Guid userId, long conversationId, ConversationType? expectedType = null, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteConversationAsync(long conversationId, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteForUserAsync(Guid userId, long conversationId, ConversationType? expectedType = null, CancellationToken ct = default);
    Task<ErrorOr<Success>> AddMemberAsync(AddMemberReqDto memberReq, CancellationToken ct = default);
    Task<ErrorOr<Success>> UpdateMemberRoleAsync(long conversationId, Guid userId, RoleType newRole, CancellationToken ct = default);
    Task<ErrorOr<Success>> TransferAdminAsync(long conversationId, Guid adminId, Guid newAdminId, CancellationToken ct = default);
}

public class ConversationCommandService(MainDbContext dbContext) : IConversationCommandService
{
    public static string GenerateDirectPairKey(Guid user1, Guid user2) =>
        (user1.CompareTo(user2) < 0) ? $"{user1}_{user2}" : $"{user2}_{user1}";

    public async Task<ErrorOr<ConversationDto>> CreateConversationAsync(CreateConversationReqDto request, CancellationToken ct = default)
    {
        var conversation = new Conversation
        {
            Type = request.Type,
            Title = request.Title,
            DirectPairKey = request.DirectPairKey,
            CreatedAt = DateTimeOffset.UtcNow,
            Members = request.InitialMembers?.ToList() ?? new List<ConversationMember>()
        };

        dbContext.Conversations.Add(conversation);
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (!string.IsNullOrEmpty(request.DirectPairKey))
        {
            // Concurrent direct conversation creation race condition
            var concurrent = await dbContext.Conversations
                .Include(c => c.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.DirectPairKey == request.DirectPairKey, ct);

            if (concurrent != null)
                return concurrent.ToDto();

            throw;
        }

        // Ensure users are loaded for members if not already populated
        await dbContext.Entry(conversation)
            .Collection(c => c.Members)
            .Query()
            .Include(m => m.User)
            .LoadAsync(ct);

        return conversation.ToDto();
    }

    public async Task<ErrorOr<Success>> JoinConversationAsync(Guid userId, long conversationId, ConversationType? expectedType = null, CancellationToken ct = default)
    {
        var conversation = await dbContext.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return Error.NotFound("Conversation.NotFound", "Conversation not found.");

        if (expectedType.HasValue && conversation.Type != expectedType.Value)
            return Error.Validation("Conversation.InvalidType", $"Expected conversation type {expectedType}, but got {conversation.Type}.");

        var member = conversation.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            return Error.NotFound("Conversation.NotInvited", "You are not invited to this conversation.");

        if (member.Status is MemberStatus.Joined)
            return Result.Success; // Idempotent success

        if (member.Status != MemberStatus.Pending)
            return Error.Conflict("Conversation.NotInvited", "A pending invitation is required to join this conversation.");

        member.Status = MemberStatus.Joined;
        member.JoinedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> RemoveFromConversationAsync(Guid userId, long conversationId, ConversationType? expectedType = null, CancellationToken ct = default)
    {
        var conversation = await dbContext.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return Error.NotFound("Conversation.NotFound", "Conversation not found.");

        if (expectedType.HasValue && conversation.Type != expectedType.Value)
            return Error.Validation("Conversation.InvalidType", $"Expected conversation type {expectedType}, but got {conversation.Type}.");

        var member = conversation.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            return Error.NotFound("Conversation.NotMember", "User is not a member of this conversation.");

        if (conversation.Type == ConversationType.Direct)
        {
            var otherMember = conversation.Members.FirstOrDefault(m => m.UserId != userId);

            // If messaging has not started (request was Pending on either side) -> remove conversation immediately
            if (member.Status == MemberStatus.Pending || (otherMember != null && otherMember.Status == MemberStatus.Pending))
            {
                dbContext.Conversations.Remove(conversation);
            }
            else
            {
                // Two-Stage Deletion: if both were Joined, mark user as Left
                member.Status = MemberStatus.Left;

                // If no joined members remain -> permanently remove conversation
                if (!conversation.Members.Any(m => m.Status == MemberStatus.Joined))
                {
                    dbContext.Conversations.Remove(conversation);
                }
            }
        }
        else // Group
        {
            conversation.Members.Remove(member);

            if (member.Role == RoleType.Admin)
            {
                var nextAdmin = conversation.Members
                    .Where(m => m.Status == MemberStatus.Joined)
                    .OrderBy(m => m.JoinedAt ?? m.CreatedAt)
                    .FirstOrDefault();

                if (nextAdmin != null)
                {
                    nextAdmin.Role = RoleType.Admin;
                }
            }

            if (!conversation.Members.Any(m => m.Status == MemberStatus.Joined))
            {
                dbContext.Conversations.Remove(conversation);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> DeleteForUserAsync(Guid userId, long conversationId, ConversationType? expectedType = null, CancellationToken ct = default)
    {
        // Keep membership/role checks valid until the deletion is committed.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var conversation = await dbContext.Conversations
            .Where(c => c.Id == conversationId && c.Members.Any(m => m.UserId == userId && m.Status == MemberStatus.Joined))
            .Select(c => new
            {
                c.Type,
                Role = c.Members.Where(m => m.UserId == userId).Select(m => m.Role).First()
            })
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
            return Error.NotFound("Conversation.NotFound", "Conversation not found or you are not an active member.");

        if (expectedType.HasValue && conversation.Type != expectedType.Value)
            return Error.Validation("Conversation.InvalidType", $"Expected conversation type {expectedType}, but got {conversation.Type}.");

        ErrorOr<Success> result;
        switch (conversation.Type)
        {
            case ConversationType.Direct:
                result = await RemoveFromConversationAsync(userId, conversationId, ConversationType.Direct, ct);
                break;
            case ConversationType.Group:
                if (conversation.Role != RoleType.Admin)
                    return Error.Forbidden("Group.Forbidden", "Only group admin can delete the group.");

                result = await DeleteConversationAsync(conversationId, ct);
                break;
            default:
                return Error.Validation("Conversation.InvalidType", "Unsupported conversation type.");
        }

        if (result.IsError)
            return result;

        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task<ErrorOr<Success>> DeleteConversationAsync(long conversationId, CancellationToken ct = default)
    {
        var conversation = await dbContext.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return Error.NotFound("Conversation.NotFound", "Conversation not found.");

        if (conversation.Type == ConversationType.Direct)
            return Error.Validation("Conversation.InvalidType", "Use RemoveFromConversation to leave a direct conversation.");

        dbContext.Conversations.Remove(conversation);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> AddMemberAsync(AddMemberReqDto memberReq, CancellationToken ct = default)
    {
        var conversation = await dbContext.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == memberReq.ConversationId, ct);

        if (conversation is null)
            return Error.NotFound("Conversation.NotFound", "Conversation not found.");

        var existingMember = conversation.Members.FirstOrDefault(m => m.UserId == memberReq.UserId);
        if (existingMember is not null)
        {
            if (existingMember.Status == MemberStatus.Joined)
                return Error.Conflict("Member.AlreadyJoined", "User is already an active member.");

            if (existingMember.Status == MemberStatus.Pending)
                return Error.Conflict("Member.AlreadyPending", "User already has a pending invitation.");

            // If previously Left: reactivate membership
            existingMember.Role = memberReq.Role;
            existingMember.Status = memberReq.Status;
            existingMember.InvitedById = memberReq.InvitedById;
            existingMember.JoinedAt = (memberReq.Status == MemberStatus.Joined) ? DateTimeOffset.UtcNow : null;
        }
        else
        {
            conversation.Members.Add(new ConversationMember
            {
                ConversationId = memberReq.ConversationId,
                UserId = memberReq.UserId,
                Role = memberReq.Role,
                Status = memberReq.Status,
                InvitedById = memberReq.InvitedById,
                JoinedAt = (memberReq.Status is MemberStatus.Joined) ? DateTimeOffset.UtcNow : null,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> UpdateMemberRoleAsync(long conversationId, Guid userId, RoleType newRole, CancellationToken ct = default)
    {
        var conversation = await dbContext.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
            return Error.NotFound("Conversation.NotFound", "Conversation not found.");

        var member = conversation.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            return Error.NotFound("Member.NotFound", "User is not a member of this conversation.");

        member.Role = newRole;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> TransferAdminAsync(long conversationId, Guid adminId, Guid newAdminId, CancellationToken ct = default)
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

        var newAdmin = conversation.Members.FirstOrDefault(m => m.UserId == newAdminId && m.Status == MemberStatus.Joined);
        if (newAdmin is null)
            return Error.Conflict("Group.TargetNotMember", "User must be an active member of the group.");

        if (adminId == newAdminId)
            return Result.Success;

        currentAdmin.Role = RoleType.Member;
        newAdmin.Role = RoleType.Admin;

        // Both role changes are persisted in the same SaveChanges transaction.
        await dbContext.SaveChangesAsync(ct);
        return Result.Success;
    }
}

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services;

public interface IConversationQueryService
{
    Task<bool> IsActiveMemberAsync(Guid userId, long conversationId, CancellationToken ct = default);
    Task<bool> CanSendMessageAsync(Guid userId, long conversationId, CancellationToken ct = default);
    Task<ErrorOr<ConversationDto>> GetConversationAsync(Guid userId, long conversationId, CancellationToken ct = default);
    Task<ErrorOr<IReadOnlyList<ConversationDto>>> GetUserConversationsAsync(Guid userId, ConversationType? type = null, CancellationToken ct = default);
    Task<IReadOnlyList<long>> GetUserConversationIdsAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<IReadOnlyList<ConversationDto>>> GetPendingConversationsAsync(Guid userId, ConversationType? type = null, CancellationToken ct = default);
}

public class ConversationQueryService(MainDbContext dbContext) : IConversationQueryService
{
    public async Task<bool> IsActiveMemberAsync(Guid userId, long conversationId, CancellationToken ct = default)
    {
        return await dbContext.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId && cm.Status == MemberStatus.Joined, ct);
    }

    public async Task<bool> CanSendMessageAsync(Guid userId, long conversationId, CancellationToken ct = default)
    {
        return await dbContext.Conversations
            .AnyAsync(c => c.Id == conversationId &&
                c.Members.Any(m => m.UserId == userId && m.Status == MemberStatus.Joined) &&
                (c.Type != ConversationType.Direct ||
                    (c.Members.Count == 2 && !c.Members.Any(m => m.Status != MemberStatus.Joined))), ct);
    }

    public async Task<ErrorOr<ConversationDto>> GetConversationAsync(Guid userId, long conversationId, CancellationToken ct = default)
    {
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.Members.Any(m => m.UserId == userId && m.Status == MemberStatus.Joined), ct);

        if (conversation == null)
            return Error.NotFound("Conversation.NotFound", "Conversation not found or you are not an active member.");

        return conversation.ToDto();
    }

    public async Task<ErrorOr<IReadOnlyList<ConversationDto>>> GetUserConversationsAsync(Guid userId, ConversationType? type = null, CancellationToken ct = default)
    {
        var query = dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.Members.Any(m => m.UserId == userId && m.Status == MemberStatus.Joined));

        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }

        var conversations = await query
            .Include(c => c.Members).ThenInclude(m => m.User)
            .ToListAsync(ct);

        return conversations
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Select(c => c.ToDto())
            .ToList();
    }

    public async Task<IReadOnlyList<long>> GetUserConversationIdsAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.ConversationMembers
            .AsNoTracking()
            .Where(cm => cm.UserId == userId && cm.Status == MemberStatus.Joined)
            .Select(cm => cm.ConversationId)
            .ToListAsync(ct);
    }

    public async Task<ErrorOr<IReadOnlyList<ConversationDto>>> GetPendingConversationsAsync(Guid userId, ConversationType? type = null, CancellationToken ct = default)
    {
        var query = dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.Members.Any(m => m.UserId == userId && m.Status == MemberStatus.Pending));

        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }

        var list = await query
            .Include(c => c.Members).ThenInclude(m => m.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return list.Select(c => c.ToDto()).ToList();
    }
}

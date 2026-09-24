using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services.MessagingServices
{
    public interface IMessagingService
    {
        Task<IReadOnlyList<PrivateMessageDto>> GetPrivateChatHistory(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default);
        Task<ErrorOr<IReadOnlyList<GroupMessageDto>>> GetGroupChatHistory(Guid currentUserId, int groupId, CancellationToken cancellationToken = default);
    }
    public class MessageRedingService(MainDbContext dbContext, IBlobStorageService blobStorageService) : IMessagingService
    {
        private string? ResolveAttachmentUrl(string? url)
        {
            if (url is null) return null;
            //for gifs
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;
            var result = blobStorageService.GenerateSasReadUrl(BlobContainers.ChatMedia, url);
            return result.IsError ? null : result.Value;
        }
        public async Task<IReadOnlyList<PrivateMessageDto>> GetPrivateChatHistory(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default)
        {
            var messages = await dbContext.PrivateMessages
                .Where(m => (m.Sender == currentUserId && m.Target == targetUserId) ||
                            (m.Sender == targetUserId && m.Target == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.id,
                    m.Sender,
                    m.Target,
                    SenderName = m.SenderUser != null
                        ? (!string.IsNullOrWhiteSpace(m.SenderUser.DisplayName) ? m.SenderUser.DisplayName : m.SenderUser.UserName)
                        : null,
                    SenderAvatarUrl = m.SenderUser != null ? m.SenderUser.ProfilePictureUrl : null,
                    m.Text,
                    m.AttachmentUrl,
                    m.AttachmentType,
                    m.SentAt
                })
                .ToListAsync(cancellationToken);

            return messages.Select(m =>
            {
                return new PrivateMessageDto
                {
                    Id = m.id,
                    SenderId = m.Sender,
                    TargetUserId = m.Target,
                    Text = m.Text,
                    AttachmentUrl = ResolveAttachmentUrl(m.AttachmentUrl),
                    AttachmentType = m.AttachmentType,
                    SentAt = m.SentAt,
                    SenderName = m.SenderName,
                    SenderAvatarUrl = m.SenderAvatarUrl
                };
            }).ToList();
        }

        public async Task<ErrorOr<IReadOnlyList<GroupMessageDto>>> GetGroupChatHistory(Guid currentUserId, int groupId, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats
                .Where(g => g.id == groupId && g.Members.Any(m => m.UserId == currentUserId))
                .Select(g => new
                {
                    Messages = g.Messages
                        .OrderBy(m => m.SentAt)
                        .Select(m => new
                        {
                            m.id,
                            m.Sender,
                            SenderName = m.SenderUser != null
                                ? (!string.IsNullOrWhiteSpace(m.SenderUser.DisplayName) ? m.SenderUser.DisplayName : m.SenderUser.UserName)
                                : null,
                            SenderAvatarUrl = m.SenderUser != null ? m.SenderUser.ProfilePictureUrl : null,
                            m.GroupId,
                            m.Text,
                            m.AttachmentUrl,
                            m.AttachmentType,
                            m.SentAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (group == null)
                return Error.NotFound(description: "Group not found");

            return group.Messages.Select(m => new GroupMessageDto
            {
                Id = m.id,
                SenderId = m.Sender,
                SenderName = m.SenderName,
                SenderAvatarUrl = m.SenderAvatarUrl,
                GroupId = m.GroupId,
                Text = m.Text,
                AttachmentUrl = ResolveAttachmentUrl(m.AttachmentUrl),
                AttachmentType = m.AttachmentType,
                SentAt = m.SentAt
            }).ToList();
        }
    }
}

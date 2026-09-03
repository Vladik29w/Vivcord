using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services.MessagingServices
{
    public interface IMessagingService
    {
        Task<IReadOnlyList<PrivateMessageDto>> GetPrivateChatHistory(Guid currentUserId, Guid targetUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GroupMessageDto>> GetGroupChatHistory(Guid currentUserId, int groupId, CancellationToken cancellationToken = default);
    }
    public class MessageRedingService(MainDbContext dbContext, IBlobStorageService blobStorageService) : IMessagingService
    {
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
                    m.Text,
                    m.AttachmentUrl,
                    m.AttachmentType,
                })
                .ToListAsync(cancellationToken);

            return messages.Select(m =>
            {
                string? sasReadUrl = null;
                if (m.AttachmentUrl is not null)
                {
                    var result = blobStorageService.GenerateSasReadUrl(BlobContainers.ChatMedia, m.AttachmentUrl);
                    sasReadUrl = result.IsError ? null : result.Value;
                }

                return new PrivateMessageDto
                {
                    Id = m.id,
                    SenderId = m.Sender,
                    TargetUserId = Guid.Empty,
                    Text = m.Text,
                    AttachmentUrl = sasReadUrl,
                    AttachmentType = m.AttachmentType
                };
            }).ToList();
        }

        public async Task<IReadOnlyList<GroupMessageDto>> GetGroupChatHistory(Guid currentUserId, int groupId, CancellationToken cancellationToken = default)
        {
            var messages = await dbContext.GroupMessages
                .Where(m => m.GroupId == groupId)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.id,
                    m.Sender,
                    SenderName = m.SenderUser != null
                        ? (!string.IsNullOrWhiteSpace(m.SenderUser.DisplayName) ? m.SenderUser.DisplayName : m.SenderUser.UserName)
                        : null,
                    m.GroupId,
                    m.Text,
                    m.AttachmentUrl,
                    m.AttachmentType,
                    m.SentAt
                })
                .ToListAsync(cancellationToken);

            return messages.Select(m =>
            {
                string? sasReadUrl = null;
                if (m.AttachmentUrl is not null)
                {
                    var result = blobStorageService.GenerateSasReadUrl(BlobContainers.ChatMedia, m.AttachmentUrl);
                    sasReadUrl = result.IsError ? null : result.Value;
                }

                return new GroupMessageDto
                {
                    Id = m.id,
                    SenderId = m.Sender,
                    SenderName = m.SenderName,
                    GroupId = m.GroupId,
                    Text = m.Text,
                    AttachmentUrl = sasReadUrl,
                    AttachmentType = m.AttachmentType
                };
            }).ToList();
        }
    }
}

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services.MessagingServices
{
    public interface IMessageReadingService
    {
        Task<ErrorOr<IReadOnlyList<MessageDto>>> GetChatHistory(long conversationId, Guid currentUserId, CancellationToken cancellationToken = default);
    }

    public class MessageReadingService(MainDbContext dbContext, IBlobStorageService blobStorageService) : IMessageReadingService
    {
        private string? ResolveAttachmentUrl(string? url)
        {
            if (url is null) return null;
            // for gifs or absolute urls
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            var result = blobStorageService.GenerateSasReadUrl(BlobContainers.ChatMedia, url);
            return result.IsError ? null : result.Value;
        }

        public async Task<ErrorOr<IReadOnlyList<MessageDto>>> GetChatHistory(
            long conversationId,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var conversationData = await dbContext.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId)
                .Select(c => new
                {
                    c.Id,
                    c.Type,
                    IsMember = c.Members.Any(m => m.UserId == currentUserId && m.Status == MemberStatus.Joined),
                    Messages = c.Messages
                        .OrderBy(m => m.Id)
                        .Select(m => new
                        {
                            m.Id,
                            m.ConversationId,
                            m.SenderId,
                            SenderName = string.IsNullOrEmpty(m.Sender.DisplayName)
                                ? m.Sender.UserName
                                : m.Sender.DisplayName,
                            SenderAvatarUrl = m.Sender.ProfilePictureUrl,
                            Text = m.Content,
                            m.AttachmentUrl,
                            m.AttachmentType,
                            m.CreatedAt,
                            m.EditedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (conversationData == null)
                return Error.NotFound("Conversation.NotFound", $"Conversation with ID {conversationId} was not found.");

            if (!conversationData.IsMember)
            {
                if (conversationData.Type == ConversationType.Group)
                {
                    return Error.Forbidden("Conversation.Forbidden", "You are not a member of this group.");
                }

                return Error.Forbidden("Conversation.Forbidden", "You are not a participant in this conversation.");
            }

            var result = conversationData.Messages.Select(m => new MessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                SenderAvatarUrl = m.SenderAvatarUrl,
                Text = m.Text,
                AttachmentUrl = ResolveAttachmentUrl(m.AttachmentUrl),
                AttachmentType = m.AttachmentType,
                CreatedAt = m.CreatedAt,
                EditedAt = m.EditedAt
            }).ToList();

            return result;
        }
    }
}

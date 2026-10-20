using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services.MessagingServices
{
    public interface IMessageSendingService
    {
        Task<ErrorOr<MessageSendResult>> SendMessageAsync(SendMessageRequestDto request, CancellationToken cancellationToken = default);
    }

    public class MessageSendingService(
        MainDbContext dbContext,
        TimeProvider timeProvider,
        IBlobStorageService blobStorageService) : IMessageSendingService
    {
        private string? ToSasUrl(string? attachmentUrl)
        {
            if (attachmentUrl is null) return null;
            // for gifs or absolute urls
            if (attachmentUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                attachmentUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return attachmentUrl;

            var result = blobStorageService.GenerateSasReadUrl(BlobContainers.ChatMedia, attachmentUrl);
            return result.IsError ? null : result.Value;
        }

        public async Task<ErrorOr<MessageSendResult>> SendMessageAsync(
            SendMessageRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var conversation = await dbContext.Conversations
                .Where(c => c.Id == request.ConversationId)
                .Select(c => new
                {
                    Entity = c,
                    SenderMember = c.Members
                        .Where(m => m.UserId == request.SenderId)
                        .Select(m => new
                        {
                            AvatarUrl = m.User.ProfilePictureUrl
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (conversation == null)
            {
                return Error.NotFound("Conversation.NotFound", $"Conversation with ID {request.ConversationId} was not found.");
            }

            if (conversation.SenderMember == null)
            {
                if (conversation.Entity.Type == ConversationType.Group)
                    return Error.Forbidden("Conversation.Forbidden", "You are not a member of this group.");

                return Error.Forbidden("Conversation.Forbidden", "You are not a participant in this conversation.");
            }

            var now = timeProvider.GetUtcNow();
            var message = new Message
            {
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                Content = request.Text,
                AttachmentUrl = request.AttachmentUrl,
                AttachmentType = request.AttachmentType,
                CreatedAt = now
            };

            dbContext.Messages.Add(message);
            conversation.Entity.LastMessageAt = now;

            await dbContext.SaveChangesAsync(cancellationToken);

            return new MessageSendResult(
                message.Id,
                ToSasUrl(request.AttachmentUrl),
                message.CreatedAt,
                request.SenderDisplayName,
                conversation.SenderMember.AvatarUrl);
        }
    }
}

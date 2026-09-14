using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services.MessagingServices
{
    public interface IMessageSendingService
    {
        Task<MessageSendResult> SendPrivateMessageAsync(PrivateMessageDto messageDto, CancellationToken cancellationToken = default);
        Task<MessageSendResult> SendGroupMessageAsync(GroupMessageDto messageDto, CancellationToken cancellationToken = default);
    }

    public class MessageSendingService(MainDbContext dbContext, TimeProvider timeProvider, IBlobStorageService blobStorageService) : IMessageSendingService
    {
        private string? ToSasUrl(string? attachmentUrl)
        {
            if (attachmentUrl is null) return null;
            //for gifs
            if (attachmentUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                attachmentUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return attachmentUrl;
            var result = blobStorageService.GenerateSasReadUrl(BlobContainers.ChatMedia, attachmentUrl);
            return result.IsError ? null : result.Value;
        }

        public async Task<MessageSendResult> SendPrivateMessageAsync(PrivateMessageDto messageDto, CancellationToken cancellationToken = default)
        {
            var userMessage = new PrivateMessage
            {
                Text = messageDto.Text,
                Sender = messageDto.SenderId,
                Target = messageDto.TargetUserId,
                SentAt = timeProvider.GetUtcNow(),
                AttachmentUrl = messageDto.AttachmentUrl,
                AttachmentType = messageDto.AttachmentType,
            };

            dbContext.PrivateMessages.Add(userMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new MessageSendResult(userMessage.id, ToSasUrl(messageDto.AttachmentUrl), userMessage.SentAt);
        }

        public async Task<MessageSendResult> SendGroupMessageAsync(GroupMessageDto messageDto, CancellationToken cancellationToken = default)
        {
            var userMessage = new GroupMessage
            {
                Text = messageDto.Text,
                Sender = messageDto.SenderId,
                GroupId = messageDto.GroupId,
                SentAt = timeProvider.GetUtcNow(),
                AttachmentUrl = messageDto.AttachmentUrl,
                AttachmentType = messageDto.AttachmentType,
            };

            dbContext.GroupMessages.Add(userMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new MessageSendResult(userMessage.id, ToSasUrl(messageDto.AttachmentUrl), userMessage.SentAt);
        }
    }
}
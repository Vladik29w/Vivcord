using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services.MessagingServices
{
    public interface IMessageSendingService
    {
        Task<PrivateMessage> SendPrivateMessageAsync(
            PrivateMessageDto messageDto,
            CancellationToken cancellationToken = default);
        Task<GroupMessage> SendGroupMessageAsync(
            GroupMessageDto messageDto,
            CancellationToken cancellationToken = default);
    }

    public class MessageSendingService(MainDbContext dbContext, TimeProvider timeProvider) : IMessageSendingService
    {
        public async Task<PrivateMessage> SendPrivateMessageAsync(PrivateMessageDto messageDto, CancellationToken cancellationToken = default)
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

            return userMessage;
        }
        public async Task<GroupMessage> SendGroupMessageAsync(GroupMessageDto messageDto, CancellationToken cancellationToken = default)
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

            return userMessage;
        }
    }
}
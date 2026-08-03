using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services.MessagingServices
{
    public interface IMessageSendingService
    {
        Task<UserMessage> SendPrivateMessageAsync(
            MessageDto messageDto,
            CancellationToken cancellationToken = default);
    }

    public class MessageSendingService(MainDbContext dbContext, TimeProvider timeProvider) : IMessageSendingService
    {
        public async Task<UserMessage> SendPrivateMessageAsync(
            MessageDto messageDto,
            CancellationToken cancellationToken = default)
        {
            var userMessage = new UserMessage
            {
                Text = messageDto.Text,
                Sender = messageDto.SenderId,
                Target = messageDto.TargetUserId,
                SentAt = timeProvider.GetUtcNow(),
                AttachmentUrl = messageDto.AttachmentUrl,
                AttachmentType = messageDto.AttachmentType,
            };

            dbContext.UserMessages.Add(userMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            return userMessage;
        }
    }
}

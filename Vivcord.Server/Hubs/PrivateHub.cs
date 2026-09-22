using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vivcord.Server.DTO;
using Vivcord.Server.Extensions;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.Server.Hubs
{
    [Authorize]
    public class PrivateHub(IMessageSendingService messageSendingService) : Hub
    {
        public async Task<int> SendMessage(SendPrivateMessageDto dto)
        {
            var senderId = Context.UserIdentifier!;
            var senderDisplayName = Context.User.GetDisplayName();

            var messageDto = new PrivateMessageDto
            {
                Id = 0,
                SenderId = Guid.Parse(senderId),
                SenderName = senderDisplayName,
                TargetUserId = dto.TargetUserId,
                Text = dto.Text,
                AttachmentUrl = dto.AttachmentUrl,
                AttachmentType = dto.AttachmentType
            };
            var savedMessage = await messageSendingService.SendPrivateMessageAsync(messageDto, Context.ConnectionAborted);

            await Clients.User(dto.TargetUserId.ToString()).SendAsync(
                "ReceiveMessage",
                senderId,
                dto.Text,
                savedMessage.Id,
                savedMessage.SasAttachmentUrl,
                dto.AttachmentType,
                senderDisplayName,
                savedMessage.SenderAvatarUrl,
                savedMessage.SentAt);

            return savedMessage.Id;
        }
    }
}

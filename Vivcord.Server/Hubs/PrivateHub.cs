using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vivcord.Server.DTO;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.Server.Hubs
{
    [Authorize]
    public class PrivateHub(IMessageSendingService messageSendingService) : Hub
    {
        public async Task<int> SendMessage(SendPrivateMessageDto dto)
        {
            var senderId = Context.UserIdentifier!;

            var messageDto = new MessageDto
            {
                Id = 0,
                SenderId = Guid.Parse(senderId),
                TargetUserId = Guid.Parse(dto.TargetUserId),
                Text = dto.Text,
                AttachmentUrl = dto.AttachmentUrl,
                AttachmentType = dto.AttachmentType
            };
            var savedMessage = await messageSendingService.SendPrivateMessageAsync(messageDto, Context.ConnectionAborted);

            await Clients.User(dto.TargetUserId).SendAsync(
                "ReceiveMessage", senderId, dto.Text, savedMessage.id, dto.AttachmentUrl, dto.AttachmentType);

            return savedMessage.id;
        }
    }
}

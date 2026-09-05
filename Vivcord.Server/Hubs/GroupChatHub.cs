using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.Server.Hubs
{
    [Authorize]
    public class GroupChatHub(IMessageSendingService messageSendingService, IGroupChatService groupChatService, MainDbContext dbContext) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId is not null && Guid.TryParse(userId, out var userGuid))
            {
                var groupsResult = await groupChatService.GetUserGroupsAsync(userGuid);
                if (!groupsResult.IsError)
                {
                    foreach (var group in groupsResult.Value)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, group.Id.ToString());
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinGroup(int groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        }

        public async Task<int> SendMessage(GroupMessageDto dto)
        {
            var senderId = Context.UserIdentifier!;
            var senderGuid = Guid.Parse(senderId);

            var senderName = Context.User?.FindFirst("displayName")?.Value
                ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? Context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName)?.Value
                ?? Context.User?.Identity?.Name
                ?? senderId;

            var messageDto = new GroupMessageDto
            {
                Id = 0,
                SenderId = senderGuid,
                SenderName = senderName,
                GroupId = dto.GroupId,
                Text = dto.Text,
                AttachmentUrl = dto.AttachmentUrl,
                AttachmentType = dto.AttachmentType
            };

            var savedMessage = await messageSendingService.SendGroupMessageAsync(
                messageDto,
                Context.ConnectionAborted);

            var senderUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == senderGuid, Context.ConnectionAborted);

            await Clients.Group(dto.GroupId.ToString()).SendAsync(
                "ReceiveMessage",
                senderId,
                dto.Text,
                savedMessage.Id,
                savedMessage.SasAttachmentUrl,
                dto.AttachmentType,
                senderName,
                senderUser?.ProfilePictureUrl);

            return savedMessage.Id;
        }
    }
}

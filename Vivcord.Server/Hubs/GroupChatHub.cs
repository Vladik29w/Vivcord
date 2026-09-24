using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vivcord.Server.DTO;
using Vivcord.Server.Extensions;
using Vivcord.Server.Services;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.Server.Hubs
{
    [Authorize]
    public class GroupChatHub(IMessageSendingService messageSendingService, IGroupChatService groupChatService) : Hub
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

        public async Task<ErrorOr<Success>> JoinGroup(int groupId)
        {
            var userId = Context.UserIdentifier;
            if (userId is null || !Guid.TryParse(userId, out var userGuid))
                return Error.Unauthorized(description: "Unauthorized");

            var isMember = await groupChatService.IsMemberAsync(userGuid, groupId);
            if (!isMember)
                return Error.Forbidden(description: "You are not a member of this group");

            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
            return Result.Success;
        }

        public async Task<ErrorOr<int>> SendMessage(SendGroupMessageDto dto)
        {
            var senderId = Context.UserIdentifier!;
            var senderGuid = Guid.Parse(senderId);
            var senderName = Context.User.GetDisplayName() ?? senderId;

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

            var sendResult = await messageSendingService.SendGroupMessageAsync(
                messageDto,
                Context.ConnectionAborted);

            if (sendResult.IsError)
                return sendResult.Errors;

            var savedMessage = sendResult.Value;

            await Clients.Group(dto.GroupId.ToString()).SendAsync(
                "ReceiveMessage",
                senderId,
                dto.Text,
                savedMessage.Id,
                savedMessage.SasAttachmentUrl,
                dto.AttachmentType,
                senderName,
                savedMessage.SenderAvatarUrl,
                savedMessage.SentAt,
                dto.GroupId);

            return savedMessage.Id;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Extensions;
using Vivcord.Server.Services.MessagingServices;
using Vivcord.Server.Services;

namespace Vivcord.Server.Hubs
{
    [Authorize]
    public class PrivateHub(
        IMessageSendingService messageSendingService,
        IUserStatusService userStatusService,
        MainDbContext dbContext) : Hub
    {
        //messaging
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

        //user status
        public override async Task OnConnectedAsync()
        {
            if (Context.User.GetUserId() is { } userId)
            {
                var becameOnline = userStatusService.UserConnect(userId, Context.ConnectionId);
                if (becameOnline)
                {
                    await NotifyFriendsStatusAsync(userId, isOnline: true);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User.GetUserId() is { } userId)
            {
                var becameOffline = userStatusService.UserDisconnect(userId, Context.ConnectionId);
                if (becameOffline)
                {
                    await NotifyFriendsStatusAsync(userId, isOnline: false);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task NotifyFriendsStatusAsync(Guid userId, bool isOnline)
        {
            var friendIds = await dbContext.UserFriends
                .Where(uf => uf.FriendId == userId || uf.UserId == userId)
                .Select(uf => uf.UserId == userId ? uf.FriendId : uf.UserId)
                .Distinct()
                .ToListAsync();

            if (friendIds.Count > 0)
            {
                var friendIdStrings = friendIds.Select(id => id.ToString()).ToList();
                await Clients.Users(friendIdStrings).SendAsync("UserStatusChanged", userId, isOnline);
            }
        }
    }
}

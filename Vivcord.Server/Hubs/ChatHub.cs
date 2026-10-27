using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vivcord.Server.DTO;
using Vivcord.Server.Extensions;
using Vivcord.Server.Services;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.Server.Hubs;

[Authorize]
public class ChatHub(
    IMessageSendingService messageSendingService,
    IConversationQueryService conversationQueryService,
    IUserStatusService userStatusService,
    IFriendService friendService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User.GetUserId() is { } userId)
        {
            var conversationIds = await conversationQueryService.GetUserConversationIdsAsync(userId, Context.ConnectionAborted);
            foreach (var convId in conversationIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, convId.ToString());
            }

            if (userStatusService.UserConnect(userId, Context.ConnectionId))
                await NotifyFriendsStatusAsync(userId, true, Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User.GetUserId() is { } userId)
        {
            var becameOffline = userStatusService.UserDisconnect(userId, Context.ConnectionId);
            if (becameOffline)
                await NotifyFriendsStatusAsync(userId, false, CancellationToken.None);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<ErrorOr<Success>> JoinConversation(long conversationId)
    {
        var userId = Context.User.GetUserId();
        if (userId == null)
            return Error.Unauthorized(description: "Unauthorized");

        var isMember = await conversationQueryService.IsActiveMemberAsync(userId.Value, conversationId);
        if (!isMember)
            return Error.Forbidden(description: "You are not an active member of this conversation.");

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        return Result.Success;
    }

    public async Task<ErrorOr<MessageSendResult>> SendMessage(SendMessageDto dto)
    {
        var userId = Context.User.GetUserId();
        if (userId == null)
            return Error.Unauthorized(description: "Unauthorized");

        var senderId = userId.Value;
        var senderDisplayName = Context.User.GetDisplayName();

        var sendResult = await messageSendingService.SendMessageAsync(
            new SendMessageRequestDto(
                dto.ConversationId,
                senderId,
                senderDisplayName,
                dto.Text,
                dto.AttachmentUrl,
                dto.AttachmentType),
            Context.ConnectionAborted);

        if (sendResult.IsError)
            return sendResult.Errors;

        var saved = sendResult.Value;

        await Clients.Group(dto.ConversationId.ToString()).SendAsync(
            "ReceiveMessage",
            senderId.ToString(),
            dto.Text,
            saved.Id.ToString(),
            saved.SasAttachmentUrl,
            dto.AttachmentType,
            senderDisplayName,
            saved.SenderAvatarUrl,
            saved.CreatedAt,
            dto.ConversationId.ToString());

        return saved;
    }

    private async Task NotifyFriendsStatusAsync(Guid userId, bool isOnline, CancellationToken cancellationToken = default)
    {
        var friendIds = await friendService.GetFriendIdsAsync(userId, cancellationToken);

        if (friendIds.Count > 0)
        {
            var friendIdStrings = friendIds.Select(id => id.ToString()).ToList();
            await Clients.Users(friendIdStrings).SendAsync("UserStatusChanged", userId, isOnline, cancellationToken);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vivcord.Server.DbContext;
using Vivcord.Server.Models;

namespace Vivcord.Server.Hubs
{
    [Authorize]
    public class PrivateHub(MainDbContext dbContext, TimeProvider timeProvider) : Hub
    {
        public async Task<int> SendMessage(string text, string targetUserId)
        {
            var senderId = Context.UserIdentifier!;
            var normalizedTargetUserId = targetUserId.ToLowerInvariant();

            var userMessage = new UserMessage
            {
                Text = text,
                Sender = Guid.Parse(senderId),
                Target = Guid.Parse(normalizedTargetUserId),
                SentAt = timeProvider.GetUtcNow()
            };
            dbContext.UserMessages.Add(userMessage);
            await dbContext.SaveChangesAsync();

            await Clients.User(normalizedTargetUserId).SendAsync("ReceiveMessage", senderId, text, userMessage.id);
            return userMessage.id;
        }
    }
}

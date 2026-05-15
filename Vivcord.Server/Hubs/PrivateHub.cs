using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vivcord.Server.DbContext;
using Vivcord.Server.Models;

namespace Vivcord.Server.Hubs
{
    [Authorize]
    public class PrivateHub(MainDbContext dbContext, TimeProvider timeProvider) : Hub
    {
        public async Task SendMessage(string text, string targetUserId)
        {
            var senderId = Context.UserIdentifier!;

            var userMessage = new UserMessage
            {
                Text = text,
                Sender = senderId,
                Target = targetUserId,
                SentAt = timeProvider.GetUtcNow()
            };
            dbContext.UserMessages.Add(userMessage);
            await dbContext.SaveChangesAsync();

            await Clients.User(targetUserId).SendAsync("ReciveMessage", senderId, text);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services
{
    public interface IMessagingService
    {
        Task<IReadOnlyList<MessageDto>> GetChatHistory(string currentUserId, string targetUserId);
    }
    public class MessagingService(MainDbContext dbContext) : IMessagingService
    {
        public async Task<IReadOnlyList<MessageDto>> GetChatHistory(string currentUserId, string targetUserId)
        {
            return await dbContext.UserMessages
                .Where(m => (m.Sender == currentUserId && m.Target == targetUserId) ||
                            (m.Sender == targetUserId && m.Target == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto(m.id, m.Sender, m.Text))
                .ToListAsync();
        }
    }
}

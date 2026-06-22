using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services
{
    public interface IMessagingService
    {
        Task<IReadOnlyList<MessageDto>> GetChatHistory(string currentUserId, string targetUserId, CancellationToken cancellationToken = default);
    }
    public class MessagingService(MainDbContext dbContext) : IMessagingService
    {
        public async Task<IReadOnlyList<MessageDto>> GetChatHistory(string currentUserId, string targetUserId, CancellationToken cancellationToken = default)
        {
            var currentGuid = Guid.Parse(currentUserId);
            var targetGuid = Guid.Parse(targetUserId);

            return await dbContext.UserMessages
                .Where(m => (m.Sender == currentGuid && m.Target == targetGuid) ||
                            (m.Sender == targetGuid && m.Target == currentGuid))
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto(m.id, m.Sender.ToString().ToLower(), m.Text))
                .ToListAsync(cancellationToken);
        }
    }
}

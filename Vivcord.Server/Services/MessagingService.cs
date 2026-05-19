using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services
{
    public interface IMessagingService
    {
        Task<IReadOnlyList<MessageDto>> GetChatHistory(string targetUser, string currentUser);
        Task<FindUserDTO?> GetProfileByUsername(string username);
    }
    public class MessagingService(MainDbContext dbContext) : IMessagingService
    {
        public async Task<FindUserDTO?> GetProfileByUsername(string username)
        {
            return await dbContext.Users
                .Where(u => u.UserName == username)
                .Select(u => new FindUserDTO
                {
                    Id = u.Id,
                    Name = u.UserName!
                })
                .FirstOrDefaultAsync();
        }
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

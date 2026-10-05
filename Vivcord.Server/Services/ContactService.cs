using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services
{
    public interface IContactService
    {
        Task<ErrorOr<FindUserDTO>> GetProfileByUsername(string username);
    }
    public class ContactService(MainDbContext dbContext, IUserStatusService userStatusService) : IContactService
    {
        public async Task<ErrorOr<FindUserDTO>> GetProfileByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Error.Validation("InvalidUsername", "Username is required");

            var user = await dbContext.Users
                .Where(u => u.UserName == username)
                .Select(u => new
                {
                    u.Id,
                    UserName = u.UserName!,
                    DisplayName = !string.IsNullOrWhiteSpace(u.DisplayName) ? u.DisplayName : u.UserName,
                    u.ProfilePictureUrl
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return Error.NotFound(description: "User not found");

            return new FindUserDTO
            {
                Id = user.Id.ToString(),
                Name = user.UserName,
                DisplayName = user.DisplayName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsOnline = userStatusService.IsUserActive(user.Id)
            };
        }
    }
}

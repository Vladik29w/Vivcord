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
    public class ContactService(MainDbContext dbContext) : IContactService
    {
        public async Task<ErrorOr<FindUserDTO>> GetProfileByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Error.Validation("InvalidUsername", "Username is required");

            var res = await dbContext.Users
                .Where(u => u.UserName == username)
                .Select(u => new FindUserDTO
                {
                    Id = u.Id.ToString(),
                    Name = u.UserName!,
                    DisplayName = !string.IsNullOrWhiteSpace(u.DisplayName) ? u.DisplayName : u.UserName,
                    ProfilePictureUrl = u.ProfilePictureUrl
                })
                .FirstOrDefaultAsync();
            if (res == null)
                return Error.NotFound(description: "User not found");

            return res;
        }
    }
}

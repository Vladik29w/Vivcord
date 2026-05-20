using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services
{
    public interface IContactService
    {
        Task<FindUserDTO?> GetProfileByUsername(string username);
    }
    public class ContactService(MainDbContext dbContext) : IContactService
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
    }
}

using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
namespace Vivcord.Server.Services
{
    public interface IProfileService
    {
        public Task ChangeUserDisplayName(Guid userId, string displayName, CancellationToken ct = default);
    }
    public class ProfileService (MainDbContext dbContext) : IProfileService
    {
        public async Task ChangeUserDisplayName(Guid userId, string displayName, CancellationToken ct = default)
        {
            await dbContext.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(u => u.SetProperty(u => u.DisplayName, displayName), ct);
        }
    }
}
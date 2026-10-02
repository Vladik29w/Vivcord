using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services
{
    public interface IFriendService
    {
        Task<ErrorOr<IReadOnlyList<FriendDTO>>> GetFriendList(Guid ownerId, CancellationToken cancellationToken = default);
        Task<ErrorOr<FriendDTO>> AddToFriendList(Guid ownerId, string userNameToAdd, CancellationToken cancellationToken = default);
        Task<ErrorOr<Success>> RemoveFromFriendList(Guid ownerId, string userNameToRemove, CancellationToken cancellationToken = default);
    }
    public class FriendService(MainDbContext dbContext) : IFriendService
    {
        public async Task<ErrorOr<IReadOnlyList<FriendDTO>>> GetFriendList(Guid ownerId, CancellationToken cancellationToken = default)
        {
            var friends = await dbContext.UserFriends
             .Where(uf => uf.UserId == ownerId)
             .Select(uf => new FriendDTO(uf.FriendId, uf.Friend.UserName!, uf.Friend.DisplayName, uf.Friend.ProfilePictureUrl))
             .ToListAsync(cancellationToken);

            return friends;
        }
        public async Task<ErrorOr<FriendDTO>> AddToFriendList(Guid ownerId, string userNameToAdd, CancellationToken cancellationToken = default)
        {
            var friend = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userNameToAdd, cancellationToken);
            if (friend == null)
                return Error.NotFound(description: $"User {userNameToAdd} not found");
            if (friend.Id == ownerId)
                return Error.Conflict(description: "You can't add yourself");

            var newFriendship = new AppUserFriend
            {
                UserId = ownerId,
                FriendId = friend.Id
            };

            dbContext.UserFriends.Add(newFriendship);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Error.Conflict(description: "Already in friend list");
            }

            return new FriendDTO(friend.Id, friend.UserName!, friend.DisplayName, friend.ProfilePictureUrl);
        }
       public async Task<ErrorOr<Success>> RemoveFromFriendList(Guid ownerId, string userNameToRemove, CancellationToken cancellationToken = default)
       {
            var deletedCount = await dbContext.UserFriends
                .Where(uf => uf.UserId == ownerId && uf.Friend.UserName == userNameToRemove)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount == 0)
                return Error.NotFound(description: "User not found or not in your friend list");

            return Result.Success;
       }
    }
}
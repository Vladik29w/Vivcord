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
        Task<ErrorOr<Success>> RemoveFromFriendList(Guid ownerId, string userNameToAdd, CancellationToken cancellationToken = default);
    }
    public class FriendService(MainDbContext dbContext) : IFriendService
    {
        public async Task<ErrorOr<IReadOnlyList<FriendDTO>>> GetFriendList(Guid ownerId, CancellationToken cancellationToken = default)
        {
            var friends = await dbContext.UserFriends
             .AsNoTracking()
             .Where(uf => uf.UserId == ownerId)
             .Select(uf => new FriendDTO(uf.FriendId, uf.Friend.UserName!, uf.Friend.ProfilePictureUrl))
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

            var alreadyFriends = await dbContext.UserFriends
                .AnyAsync(uf => uf.UserId == ownerId && uf.FriendId == friend.Id, cancellationToken);

            if (alreadyFriends)
                return Error.Conflict(description: "Already in friend list");

            var newFriendship = new AppUserFriend
            {
                UserId = ownerId,
                FriendId = friend.Id
            };

            dbContext.UserFriends.Add(newFriendship);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new FriendDTO(friend.Id, friend.UserName!, friend.ProfilePictureUrl);
        }
        public async Task<ErrorOr<Success>> RemoveFromFriendList(Guid ownerId, string userNameToRemove, CancellationToken cancellationToken = default)
        {
            var friend = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userNameToRemove, cancellationToken);
            if (friend == null)
                return Error.NotFound(description: $"User {userNameToRemove} not found");

            var friendship = await dbContext.UserFriends
                .FirstOrDefaultAsync(uf => uf.UserId == ownerId && uf.FriendId == friend.Id, cancellationToken);

            if (friendship == null)
                return Error.NotFound(description: "This user is not in your friend list");

            dbContext.UserFriends.Remove(friendship);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }
    }
}
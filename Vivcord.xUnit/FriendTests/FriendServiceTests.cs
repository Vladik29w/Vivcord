using ErrorOr;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.Models;
using Vivcord.Server.Services;

namespace Vivcord.xUnit.FriendTests
{
    public class FriendServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly MainDbContext _db;

        public FriendServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new MainDbContext(options);
            _db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private async Task<AppUser> CreateUserAsync(string username, string displayName = "", string? profilePictureUrl = null)
        {
            var normalizedUsername = username.ToUpperInvariant();
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = username,
                NormalizedUserName = normalizedUsername,
                Email = $"{username}@test.com",
                NormalizedEmail = $"{normalizedUsername}@TEST.COM",
                DisplayName = displayName,
                ProfilePictureUrl = profilePictureUrl
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task GetFriendList_WhenOwnerHasNoFriends_ReturnsEmptyList()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var service = new FriendService(_db);

            // Act
            var result = await service.GetFriendList(owner.Id);

            // Assert
            Assert.False(result.IsError);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetFriendList_WhenFriendsExist_ReturnsTheirProfileDetails()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var friend = await CreateUserAsync("friend", "Friend Display", "https://example.com/friend.png");
            _db.UserFriends.Add(new AppUserFriend { UserId = owner.Id, FriendId = friend.Id });
            await _db.SaveChangesAsync();
            var service = new FriendService(_db);

            // Act
            var result = await service.GetFriendList(owner.Id);

            // Assert
            Assert.False(result.IsError);
            var dto = Assert.Single(result.Value);
            Assert.Equal(friend.Id, dto.Id);
            Assert.Equal("friend", dto.UserName);
            Assert.Equal("Friend Display", dto.DisplayName);
            Assert.Equal("https://example.com/friend.png", dto.ProfilePictureUrl);
        }

        [Fact]
        public async Task AddToFriendList_WhenUsernameDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var service = new FriendService(_db);

            // Act
            var result = await service.AddToFriendList(owner.Id, "missing");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("User missing not found", result.FirstError.Description);
        }

        [Fact]
        public async Task AddToFriendList_WhenAddingSelf_ReturnsConflict()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var service = new FriendService(_db);

            // Act
            var result = await service.AddToFriendList(owner.Id, "owner");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
            Assert.Equal("You can't add yourself", result.FirstError.Description);
            Assert.Empty(await _db.UserFriends.ToListAsync());
        }

        [Fact]
        public async Task AddToFriendList_WhenValidUserIsAdded_PersistsFriendshipAndReturnsProfile()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var friend = await CreateUserAsync("friend", "Friend Display", "https://example.com/friend.png");
            var service = new FriendService(_db);

            // Act
            var result = await service.AddToFriendList(owner.Id, "friend");

            // Assert
            Assert.False(result.IsError);
            Assert.Equal(friend.Id, result.Value.Id);
            Assert.Equal("friend", result.Value.UserName);
            Assert.Equal("Friend Display", result.Value.DisplayName);
            Assert.Equal("https://example.com/friend.png", result.Value.ProfilePictureUrl);
            var savedFriendship = await _db.UserFriends.AsNoTracking().SingleAsync();
            Assert.Equal(owner.Id, savedFriendship.UserId);
            Assert.Equal(friend.Id, savedFriendship.FriendId);
        }

        [Fact]
        public async Task AddToFriendList_WhenFriendshipAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            await CreateUserAsync("friend");
            var service = new FriendService(_db);
            await service.AddToFriendList(owner.Id, "friend");
            _db.ChangeTracker.Clear();

            // Act
            var result = await service.AddToFriendList(owner.Id, "friend");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
            Assert.Equal("Already in friend list", result.FirstError.Description);
            Assert.Equal(1, await _db.UserFriends.AsNoTracking().CountAsync());
        }

        [Fact]
        public async Task RemoveFromFriendList_WhenFriendshipExists_DeletesIt()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var friend = await CreateUserAsync("friend");
            _db.UserFriends.Add(new AppUserFriend { UserId = owner.Id, FriendId = friend.Id });
            await _db.SaveChangesAsync();
            var service = new FriendService(_db);

            // Act
            var result = await service.RemoveFromFriendList(owner.Id, "friend");

            // Assert
            Assert.False(result.IsError);
            Assert.Empty(await _db.UserFriends.AsNoTracking().ToListAsync());
        }

        [Fact]
        public async Task RemoveFromFriendList_WhenUserIsNotInFriendList_ReturnsNotFound()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            await CreateUserAsync("not-a-friend");
            var service = new FriendService(_db);

            // Act
            var result = await service.RemoveFromFriendList(owner.Id, "not-a-friend");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("User not found or not in your friend list", result.FirstError.Description);
        }

        [Fact]
        public async Task RemoveFromFriendList_WhenUsernameDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var service = new FriendService(_db);

            // Act
            var result = await service.RemoveFromFriendList(owner.Id, "missing");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}

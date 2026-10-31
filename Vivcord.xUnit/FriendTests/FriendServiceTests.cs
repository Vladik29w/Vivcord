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
        private readonly IConversationCommandService _commandService;
        private readonly IFriendService _friendService;

        public FriendServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new MainDbContext(options);
            _db.Database.EnsureCreated();

            _commandService = new ConversationCommandService(_db);
            _friendService = new FriendService(_db, _commandService);
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
        public async Task SendFriendRequest_CreatesPendingDirectConversation_SenderIsJoined_TargetIsPending()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var target = await CreateUserAsync("bob");

            // Act
            var result = await _friendService.SendFriendRequestAsync(sender.Id, "bob");

            // Assert
            Assert.False(result.IsError);
            Assert.Equal("Direct", result.Value.Type);

            var conv = await _db.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == result.Value.Id);
            Assert.NotNull(conv);
            Assert.Equal(2, conv.Members.Count);

            var senderMember = conv.Members.First(m => m.UserId == sender.Id);
            Assert.Equal(MemberStatus.Joined, senderMember.Status);
            Assert.NotNull(senderMember.JoinedAt);

            var targetMember = conv.Members.First(m => m.UserId == target.Id);
            Assert.Equal(MemberStatus.Pending, targetMember.Status);
            Assert.Equal(sender.Id, targetMember.InvitedById);
            Assert.Null(targetMember.JoinedAt);
        }

        [Fact]
        public async Task SendFriendRequest_WhenSelf_ReturnsConflict()
        {
            // Arrange
            var user = await CreateUserAsync("alice");

            // Act
            var result = await _friendService.SendFriendRequestAsync(user.Id, "alice");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        }

        [Fact]
        public async Task SendFriendRequest_WhenTargetNotFound_ReturnsNotFound()
        {
            // Arrange
            var user = await CreateUserAsync("alice");

            // Act
            var result = await _friendService.SendFriendRequestAsync(user.Id, "unknown");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task GetFriendList_WhenOnlyPending_ReturnsEmpty()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var target = await CreateUserAsync("bob");
            await _friendService.SendFriendRequestAsync(sender.Id, "bob");

            // Act: sender checks friends
            var senderFriends = await _friendService.GetFriendListAsync(sender.Id);
            // Act: target checks friends
            var targetFriends = await _friendService.GetFriendListAsync(target.Id);

            // Assert: neither sees each other as friends yet!
            Assert.False(senderFriends.IsError);
            Assert.Empty(senderFriends.Value);

            Assert.False(targetFriends.IsError);
            Assert.Empty(targetFriends.Value);
        }

        [Fact]
        public async Task AcceptFriendRequest_SetsTargetToJoined_ConversationBecomesActiveForBoth()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var target = await CreateUserAsync("bob", "Bob Display", "https://example.com/bob.png");
            var sendResult = await _friendService.SendFriendRequestAsync(sender.Id, "bob");

            // Act: bob accepts
            var acceptResult = await _friendService.AcceptFriendRequestAsync(target.Id, sendResult.Value.Id);

            // Assert
            Assert.False(acceptResult.IsError);

            var senderFriends = await _friendService.GetFriendListAsync(sender.Id);
            Assert.False(senderFriends.IsError);
            var friendOfAlice = Assert.Single(senderFriends.Value);
            Assert.Equal(target.Id, friendOfAlice.Id);
            Assert.Equal("bob", friendOfAlice.UserName);
            Assert.Equal("Bob Display", friendOfAlice.DisplayName);

            var targetFriends = await _friendService.GetFriendListAsync(target.Id);
            Assert.False(targetFriends.IsError);
            var friendOfBob = Assert.Single(targetFriends.Value);
            Assert.Equal(sender.Id, friendOfBob.Id);
            Assert.Equal("alice", friendOfBob.UserName);
        }

        [Fact]
        public async Task DeclineFriendRequest_DeletesConversationEntirely()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var target = await CreateUserAsync("bob");
            var sendResult = await _friendService.SendFriendRequestAsync(sender.Id, "bob");

            // Act: bob declines
            var declineResult = await _friendService.DeclineFriendRequestAsync(target.Id, sendResult.Value.Id);

            // Assert
            Assert.False(declineResult.IsError);
            var convExists = await _db.Conversations.AnyAsync(c => c.Id == sendResult.Value.Id);
            Assert.False(convExists);
        }

        [Fact]
        public async Task CancelFriendRequest_DeletesConversationEntirely()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var target = await CreateUserAsync("bob");
            var sendResult = await _friendService.SendFriendRequestAsync(sender.Id, "bob");

            // Act: alice cancels
            var cancelResult = await _friendService.CancelFriendRequestAsync(sender.Id, sendResult.Value.Id);

            // Assert
            Assert.False(cancelResult.IsError);
            var convExists = await _db.Conversations.AnyAsync(c => c.Id == sendResult.Value.Id);
            Assert.False(convExists);
        }

        [Fact]
        public async Task RemoveFriend_WhenHistoryExists_SetsMemberToLeft_DoesNotDeleteMessages()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var target = await CreateUserAsync("bob");
            var sendResult = await _friendService.SendFriendRequestAsync(sender.Id, "bob");
            await _friendService.AcceptFriendRequestAsync(target.Id, sendResult.Value.Id);

            // Add a message
            _db.Messages.Add(new Message
            {
                Id = 12345,
                ConversationId = sendResult.Value.Id,
                SenderId = sender.Id,
                Content = "Hello Bob!",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync();

            // Act: alice removes bob
            var removeResult = await _friendService.RemoveFriendAsync(sender.Id, "bob");

            // Assert
            Assert.False(removeResult.IsError);

            // Conversation and message are NOT purged because Bob is still Joined!
            var conv = await _db.Conversations.Include(c => c.Members).Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == sendResult.Value.Id);
            Assert.NotNull(conv);
            Assert.Single(conv.Messages);

            var aliceMember = conv.Members.First(m => m.UserId == sender.Id);
            Assert.Equal(MemberStatus.Left, aliceMember.Status);

            var bobMember = conv.Members.First(m => m.UserId == target.Id);
            Assert.Equal(MemberStatus.Joined, bobMember.Status);

            // Alice no longer sees Bob in her friend list, but Bob STILL sees Alice until Bob also removes her
            var aliceFriends = await _friendService.GetFriendListAsync(sender.Id);
            Assert.False(aliceFriends.IsError);
            Assert.Empty(aliceFriends.Value);

            var bobFriends = await _friendService.GetFriendListAsync(target.Id);
            Assert.False(bobFriends.IsError);
            var remainingFriendForBob = Assert.Single(bobFriends.Value);
            Assert.Equal("alice", remainingFriendForBob.UserName);
        }

        [Fact]
        public async Task RemoveFriend_WhenSecondUserAlsoRemoves_PurgesConversationFromDb()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var target = await CreateUserAsync("bob");
            var sendResult = await _friendService.SendFriendRequestAsync(sender.Id, "bob");
            await _friendService.AcceptFriendRequestAsync(target.Id, sendResult.Value.Id);

            // Alice removes Bob
            await _friendService.RemoveFriendAsync(sender.Id, "bob");

            // Act: Bob now also removes/leaves the conversation
            var bobLeaveResult = await _commandService.RemoveFromConversationAsync(target.Id, sendResult.Value.Id, ConversationType.Direct);

            // Assert
            Assert.False(bobLeaveResult.IsError);

            // Since 0 active Joined members remain, the conversation is purged!
            var convExists = await _db.Conversations.AnyAsync(c => c.Id == sendResult.Value.Id);
            Assert.False(convExists);
        }

        [Fact]
        public async Task GetIncomingAndOutgoingRequests_WorkCorrectly()
        {
            // Arrange
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var charlie = await CreateUserAsync("charlie");

            // Alice requests Bob
            await _friendService.SendFriendRequestAsync(alice.Id, "bob");
            // Charlie requests Alice
            await _friendService.SendFriendRequestAsync(charlie.Id, "alice");

            // Act
            var aliceIncoming = await _friendService.GetIncomingRequestsAsync(alice.Id);
            var aliceOutgoing = await _friendService.GetOutgoingRequestsAsync(alice.Id);

            // Assert
            Assert.False(aliceIncoming.IsError);
            Assert.Single(aliceIncoming.Value);
            Assert.Equal("charlie", aliceIncoming.Value[0].UserName);

            Assert.False(aliceOutgoing.IsError);
            Assert.Single(aliceOutgoing.Value);
            Assert.Equal("bob", aliceOutgoing.Value[0].UserName);
        }

        [Fact]
        public async Task GetFriendIdsAsync_ReturnsOnlyJoinedFriendGuids()
        {
            // Arrange
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var charlie = await CreateUserAsync("charlie");

            // Alice and Bob are friends (accepted)
            var req1 = await _friendService.SendFriendRequestAsync(alice.Id, "bob");
            await _friendService.AcceptFriendRequestAsync(bob.Id, req1.Value.Id);

            // Alice and Charlie: pending
            await _friendService.SendFriendRequestAsync(alice.Id, "charlie");

            // Act
            var friendIds = await _friendService.GetFriendIdsAsync(alice.Id);

            // Assert: only Bob
            Assert.Single(friendIds);
            Assert.Equal(bob.Id, friendIds[0]);
        }
    }
}

using ErrorOr;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;
using Vivcord.Server.Services;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.xUnit.MessagingTests
{
    public class MessageSendingServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly MainDbContext _db;
        private readonly IConversationCommandService _commandService;
        private readonly IConversationQueryService _queryService;
        private readonly IMessageSendingService _messagingService;

        public MessageSendingServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<MainDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new MainDbContext(options);
            _db.Database.EnsureCreated();

            _commandService = new ConversationCommandService(_db);
            _queryService = new ConversationQueryService(_db);

            var blobStorageMock = new Mock<IBlobStorageService>();
            _messagingService = new MessageSendingService(_db, TimeProvider.System, blobStorageMock.Object, _queryService);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private async Task<AppUser> CreateUserAsync(string username)
        {
            var normalizedUsername = username.ToUpperInvariant();
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = username,
                NormalizedUserName = normalizedUsername,
                Email = $"{username}@test.com",
                NormalizedEmail = $"{normalizedUsername}@TEST.COM"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        private async Task<ConversationDto> CreateDirectConversationAsync(AppUser user1, AppUser user2)
        {
            var pairKey = ConversationCommandService.GenerateDirectPairKey(user1.Id, user2.Id);
            var result = await _commandService.CreateConversationAsync(new CreateConversationReqDto(
                Type: ConversationType.Direct,
                DirectPairKey: pairKey,
                InitialMembers:
                [
                    new ConversationMember { UserId = user1.Id, Role = RoleType.Member, Status = MemberStatus.Joined, JoinedAt = DateTimeOffset.UtcNow },
                    new ConversationMember { UserId = user2.Id, Role = RoleType.Member, Status = MemberStatus.Pending, InvitedById = user1.Id, JoinedAt = null }
                ]
            ));
            return result.Value;
        }

        private async Task<ConversationDto> CreateGroupConversationAsync(AppUser owner, string title = "Developers")
        {
            var result = await _commandService.CreateConversationAsync(new CreateConversationReqDto(
                Type: ConversationType.Group,
                Title: title,
                InitialMembers:
                [
                    new ConversationMember { UserId = owner.Id, Role = RoleType.Admin, Status = MemberStatus.Joined, JoinedAt = DateTimeOffset.UtcNow }
                ]
            ));
            return result.Value;
        }

        [Fact]
        public async Task SendMessageAsync_WhenDirectChatPending_ReturnsForbidden()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var target = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(sender, target);

            var request = new SendMessageRequestDto(direct.Id, sender.Id, "Alice", "Hello before accept!");

            // Act
            var sendResult = await _messagingService.SendMessageAsync(request);

            // Assert
            Assert.True(sendResult.IsError);
            Assert.Equal(ErrorType.Forbidden, sendResult.FirstError.Type);
        }

        [Fact]
        public async Task SendMessageAsync_WhenBothJoined_SavesMessageAndUpdatesLastMessageAt()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var target = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(sender, target);
            await _commandService.JoinConversationAsync(target.Id, direct.Id, ConversationType.Direct);

            var request = new SendMessageRequestDto(direct.Id, sender.Id, "Alice", "Hello Bob!");

            // Act
            var sendResult = await _messagingService.SendMessageAsync(request);

            // Assert
            Assert.False(sendResult.IsError);

            var conv = await _db.Conversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == direct.Id);
            Assert.NotNull(conv);
            Assert.NotNull(conv.LastMessageAt);
            var savedMsg = Assert.Single(conv.Messages);
            Assert.Equal("Hello Bob!", savedMsg.Content);
            Assert.Equal(sender.Id, savedMsg.SenderId);
        }

        [Fact]
        public async Task SendMessageAsync_WhenSenderNotMember_ReturnsForbidden()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var outsider = await CreateUserAsync("outsider");
            var group = await CreateGroupConversationAsync(owner, "Developers");

            var request = new SendMessageRequestDto(group.Id, outsider.Id, "Outsider", "Spam!");

            // Act
            var sendResult = await _messagingService.SendMessageAsync(request);

            // Assert
            Assert.True(sendResult.IsError);
            Assert.Equal(ErrorType.Forbidden, sendResult.FirstError.Type);
        }

        [Fact]
        public async Task SendMessageAsync_WhenConversationNotFound_ReturnsForbiddenOrNotFound()
        {
            // Arrange
            var sender = await CreateUserAsync("alice");
            var request = new SendMessageRequestDto(999999, sender.Id, "Alice", "Ghost!");

            // Act
            var sendResult = await _messagingService.SendMessageAsync(request);

            // Assert: CanSendMessageAsync returns false -> Forbidden
            Assert.True(sendResult.IsError);
        }
    }
}

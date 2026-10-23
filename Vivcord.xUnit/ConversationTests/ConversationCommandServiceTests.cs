using System.Security.Claims;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Vivcord.Server.Controllers;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;
using Vivcord.Server.Services;

namespace Vivcord.xUnit.ConversationTests
{
    public class ConversationCommandServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly MainDbContext _db;
        private readonly IConversationCommandService _commandService;
        private readonly IConversationQueryService _queryService;

        public ConversationCommandServiceTests()
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

        private async Task<ConversationDto> CreateDirectConversationAsync(AppUser user1, AppUser user2, MemberStatus user2Status = MemberStatus.Pending)
        {
            var pairKey = ConversationCommandService.GenerateDirectPairKey(user1.Id, user2.Id);
            var result = await _commandService.CreateConversationAsync(new CreateConversationReqDto(
                Type: ConversationType.Direct,
                DirectPairKey: pairKey,
                InitialMembers:
                [
                    new ConversationMember { UserId = user1.Id, Role = RoleType.Member, Status = MemberStatus.Joined, JoinedAt = DateTimeOffset.UtcNow },
                    new ConversationMember { UserId = user2.Id, Role = RoleType.Member, Status = user2Status, InvitedById = user1.Id, JoinedAt = user2Status == MemberStatus.Joined ? DateTimeOffset.UtcNow : null }
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
        public async Task JoinConversation_WhenExpectedTypeMismatch_ReturnsValidation()
        {
            // Arrange: create a Group
            var owner = await CreateUserAsync("owner");
            var guest = await CreateUserAsync("guest");
            var group = await CreateGroupConversationAsync(owner);
            await _commandService.AddMemberAsync(new AddMemberReqDto(group.Id, guest.Id, RoleType.Member, MemberStatus.Pending, owner.Id));

            // Act: try to join with expectedType = Direct (e.g. from friend endpoint)
            var joinResult = await _commandService.JoinConversationAsync(guest.Id, group.Id, ConversationType.Direct);

            // Assert
            Assert.True(joinResult.IsError);
            Assert.Equal(ErrorType.Validation, joinResult.FirstError.Type);
        }

        [Fact]
        public async Task RemoveFromConversation_WhenExpectedTypeMismatch_ReturnsValidation()
        {
            // Arrange: create a Direct conversation
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob);

            // Act: try to remove with expectedType = Group (e.g. from group endpoint)
            var leaveResult = await _commandService.RemoveFromConversationAsync(alice.Id, direct.Id, ConversationType.Group);

            // Assert
            Assert.True(leaveResult.IsError);
            Assert.Equal(ErrorType.Validation, leaveResult.FirstError.Type);
        }

        [Fact]
        public async Task CanSendMessage_DirectWhenPending_ReturnsFalse()
        {
            // Arrange
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob);

            // Act
            var aliceCanSend = await _queryService.CanSendMessageAsync(alice.Id, direct.Id);
            var bobCanSend = await _queryService.CanSendMessageAsync(bob.Id, direct.Id);

            // Assert: neither can send until accepted!
            Assert.False(aliceCanSend);
            Assert.False(bobCanSend);
        }

        [Fact]
        public async Task CanSendMessage_DirectWhenBothJoined_ReturnsTrue()
        {
            // Arrange
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob);
            await _commandService.JoinConversationAsync(bob.Id, direct.Id, ConversationType.Direct);

            // Act
            var aliceCanSend = await _queryService.CanSendMessageAsync(alice.Id, direct.Id);
            var bobCanSend = await _queryService.CanSendMessageAsync(bob.Id, direct.Id);

            // Assert: both can send now!
            Assert.True(aliceCanSend);
            Assert.True(bobCanSend);
        }

        [Fact]
        public async Task CanSendMessage_DirectWhenOneLeft_ReturnsFalse()
        {
            // Arrange
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob);
            await _commandService.JoinConversationAsync(bob.Id, direct.Id, ConversationType.Direct);

            // Alice leaves
            await _commandService.RemoveFromConversationAsync(alice.Id, direct.Id, ConversationType.Direct);

            // Act
            var aliceCanSend = await _queryService.CanSendMessageAsync(alice.Id, direct.Id);
            var bobCanSend = await _queryService.CanSendMessageAsync(bob.Id, direct.Id);

            // Assert
            Assert.False(aliceCanSend);
            Assert.False(bobCanSend);
        }

        [Fact]
        public async Task GetUserConversations_DirectWhenPending_IncludedOnlyForJoinedUser()
        {
            // Arrange
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob);

            // Act
            var aliceConversations = await _queryService.GetUserConversationsAsync(alice.Id);
            var bobConversations = await _queryService.GetUserConversationsAsync(bob.Id);

            // Assert: Alice (Joined) sees conversation in her list, Bob (Pending) does not!
            Assert.False(aliceConversations.IsError);
            var aliceConv = Assert.Single(aliceConversations.Value);
            Assert.Equal(direct.Id, aliceConv.Id);

            Assert.False(bobConversations.IsError);
            Assert.Empty(bobConversations.Value);
        }

        [Fact]
        public async Task AddMemberAndRoleUpdate_WorkCorrectly()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var group = await CreateGroupConversationAsync(owner);

            // Act: Add member
            var addResult = await _commandService.AddMemberAsync(new AddMemberReqDto(group.Id, member.Id, RoleType.Member, MemberStatus.Joined, owner.Id));
            Assert.False(addResult.IsError);

            // Update role to Admin
            var roleResult = await _commandService.UpdateMemberRoleAsync(group.Id, member.Id, RoleType.Admin);
            Assert.False(roleResult.IsError);

            // Assert
            var convMember = await _db.ConversationMembers.FirstOrDefaultAsync(cm => cm.ConversationId == group.Id && cm.UserId == member.Id);
            Assert.NotNull(convMember);
            Assert.Equal(RoleType.Admin, convMember.Role);
            Assert.Equal(MemberStatus.Joined, convMember.Status);
        }

        [Fact]
        public async Task JoinConversation_WhenMemberLeft_ReturnsConflictWithoutRestoringMembership()
        {
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob, MemberStatus.Joined);
            await _commandService.RemoveFromConversationAsync(alice.Id, direct.Id, ConversationType.Direct);
            _db.ChangeTracker.Clear();

            var result = await _commandService.JoinConversationAsync(alice.Id, direct.Id, ConversationType.Direct);

            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
            _db.ChangeTracker.Clear();
            var member = await _db.ConversationMembers.SingleAsync(m => m.ConversationId == direct.Id && m.UserId == alice.Id);
            Assert.Equal(MemberStatus.Left, member.Status);
            Assert.False(await _queryService.CanSendMessageAsync(alice.Id, direct.Id));
            Assert.False(await _queryService.CanSendMessageAsync(bob.Id, direct.Id));
        }

        [Fact]
        public async Task JoinConversation_WhenAlreadyJoined_PreservesJoinedAt()
        {
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob, MemberStatus.Joined);
            var joinedAt = (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == direct.Id && m.UserId == alice.Id)).JoinedAt;

            var result = await _commandService.JoinConversationAsync(alice.Id, direct.Id);

            Assert.False(result.IsError);
            _db.ChangeTracker.Clear();
            Assert.Equal(joinedAt, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == direct.Id && m.UserId == alice.Id)).JoinedAt);
        }

        [Fact]
        public async Task DeleteConversation_Direct_ReturnsValidationWithoutDeletingHistory()
        {
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob, MemberStatus.Joined);
            _db.Messages.Add(new Message { ConversationId = direct.Id, SenderId = alice.Id, Content = "History" });
            await _db.SaveChangesAsync();

            var result = await _commandService.DeleteConversationAsync(direct.Id);

            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Validation, result.FirstError.Type);
            Assert.True(await _db.Conversations.AnyAsync(c => c.Id == direct.Id));
            Assert.True(await _db.Messages.AnyAsync(m => m.ConversationId == direct.Id));
        }

        [Fact]
        public async Task DeleteEndpoint_Direct_PreservesHistoryUntilBothMembersLeave()
        {
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob, MemberStatus.Joined);
            _db.Messages.Add(new Message { ConversationId = direct.Id, SenderId = alice.Id, Content = "History" });
            await _db.SaveChangesAsync();

            var aliceResult = await CreateController(alice.Id).DeleteConversation(direct.Id, CancellationToken.None);

            Assert.IsType<OkResult>(aliceResult);
            _db.ChangeTracker.Clear();
            Assert.True(await _db.Conversations.AnyAsync(c => c.Id == direct.Id));
            Assert.True(await _db.Messages.AnyAsync(m => m.ConversationId == direct.Id));
            Assert.Equal(MemberStatus.Left, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == direct.Id && m.UserId == alice.Id)).Status);
            Assert.Equal(MemberStatus.Joined, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == direct.Id && m.UserId == bob.Id)).Status);

            var bobResult = await CreateController(bob.Id).DeleteConversation(direct.Id, CancellationToken.None);

            Assert.IsType<OkResult>(bobResult);
            Assert.False(await _db.Conversations.AnyAsync(c => c.Id == direct.Id));
            Assert.False(await _db.Messages.AnyAsync(m => m.ConversationId == direct.Id));
        }

        [Fact]
        public async Task DeleteEndpoint_GroupMember_ReturnsForbiddenWithoutDeletingGroup()
        {
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var group = await CreateGroupConversationAsync(owner);
            await _commandService.AddMemberAsync(new AddMemberReqDto(group.Id, member.Id, Status: MemberStatus.Joined));

            var result = await CreateController(member.Id).DeleteConversation(group.Id, CancellationToken.None);

            Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result).StatusCode);
            Assert.True(await _db.Conversations.AnyAsync(c => c.Id == group.Id));
        }

        [Fact]
        public async Task DeleteEndpoint_GroupAdmin_DeletesGroupWithinSerializableTransaction()
        {
            var owner = await CreateUserAsync("owner");
            var group = await CreateGroupConversationAsync(owner);
            _db.Messages.Add(new Message { ConversationId = group.Id, SenderId = owner.Id, Content = "History" });
            await _db.SaveChangesAsync();
            _db.SavingChanges += (_, _) =>
            {
                Assert.NotNull(_db.Database.CurrentTransaction);
                Assert.Equal(System.Data.IsolationLevel.Serializable, _db.Database.CurrentTransaction.GetDbTransaction().IsolationLevel);
            };

            var result = await CreateController(owner.Id).DeleteConversation(group.Id, CancellationToken.None);

            Assert.IsType<OkResult>(result);
            Assert.False(await _db.Conversations.AnyAsync(c => c.Id == group.Id));
            Assert.False(await _db.Messages.AnyAsync(m => m.ConversationId == group.Id));
            Assert.Null(_db.Database.CurrentTransaction);
        }

        [Theory]
        [InlineData(MemberStatus.Pending)]
        [InlineData(MemberStatus.Left)]
        public async Task DeleteForUser_InactiveAdmin_ReturnsNotFound(MemberStatus status)
        {
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var group = await CreateGroupConversationAsync(owner);
            await _commandService.AddMemberAsync(new AddMemberReqDto(group.Id, member.Id, RoleType.Admin, status));

            var result = await _commandService.DeleteForUserAsync(member.Id, group.Id);

            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.True(await _db.Conversations.AnyAsync(c => c.Id == group.Id));
            Assert.Null(_db.Database.CurrentTransaction);
        }

        [Fact]
        public async Task DeleteForUser_NonMemberOrMissingConversation_ReturnsNotFound()
        {
            var owner = await CreateUserAsync("owner");
            var outsider = await CreateUserAsync("outsider");
            var group = await CreateGroupConversationAsync(owner);

            var outsiderResult = await _commandService.DeleteForUserAsync(outsider.Id, group.Id);
            var missingResult = await _commandService.DeleteForUserAsync(owner.Id, long.MaxValue);

            Assert.True(outsiderResult.IsError);
            Assert.Equal(ErrorType.NotFound, outsiderResult.FirstError.Type);
            Assert.True(missingResult.IsError);
            Assert.Equal(ErrorType.NotFound, missingResult.FirstError.Type);
            Assert.True(await _db.Conversations.AnyAsync(c => c.Id == group.Id));
        }

        [Fact]
        public async Task DeleteForUser_WhenTrackedAdminRoleIsStale_UsesCurrentDatabaseRole()
        {
            var owner = await CreateUserAsync("owner");
            var group = await CreateGroupConversationAsync(owner);
            var membership = await _db.ConversationMembers.SingleAsync(m => m.ConversationId == group.Id && m.UserId == owner.Id);
            await _db.ConversationMembers
                .Where(m => m.ConversationId == group.Id && m.UserId == owner.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.Role, RoleType.Member));
            Assert.Equal(RoleType.Admin, membership.Role);

            var result = await _commandService.DeleteForUserAsync(owner.Id, group.Id);

            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            Assert.True(await _db.Conversations.AnyAsync(c => c.Id == group.Id));
        }

        [Fact]
        public async Task DeleteForUser_WhenExpectedTypeMismatch_DoesNotRemoveDirectMembership()
        {
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob, MemberStatus.Joined);

            var result = await _commandService.DeleteForUserAsync(alice.Id, direct.Id, ConversationType.Group);

            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Validation, result.FirstError.Type);
            Assert.True(await _queryService.CanSendMessageAsync(alice.Id, direct.Id));
        }

        private ConversationController CreateController(Guid userId)
        {
            var problemDetailsFactory = new Mock<ProblemDetailsFactory>();
            problemDetailsFactory
                .Setup(f => f.CreateProblemDetails(It.IsAny<HttpContext>(), It.IsAny<int?>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((HttpContext context, int? status, string? title, string? type, string? detail, string? instance) =>
                    new ProblemDetails { Status = status, Title = title });

            return new ConversationController(_queryService, _commandService)
            {
                ProblemDetailsFactory = problemDetailsFactory.Object,
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"))
                    }
                }
            };
        }

        [Theory]
        [InlineData(MemberStatus.Joined, true)]
        [InlineData(MemberStatus.Pending, false)]
        [InlineData(MemberStatus.Left, false)]
        public async Task CanSendMessage_Group_RequiresJoinedMembershipWithoutLoadingEntities(MemberStatus status, bool expected)
        {
            var owner = await CreateUserAsync("owner");
            var guest = await CreateUserAsync("guest");
            var group = await CreateGroupConversationAsync(owner);
            await _commandService.AddMemberAsync(new AddMemberReqDto(group.Id, guest.Id, Status: status));
            _db.ChangeTracker.Clear();

            Assert.Equal(expected, await _queryService.CanSendMessageAsync(guest.Id, group.Id));
            Assert.True(await _queryService.CanSendMessageAsync(owner.Id, group.Id));
            Assert.False(await _queryService.CanSendMessageAsync(Guid.NewGuid(), group.Id));
            Assert.False(await _queryService.CanSendMessageAsync(owner.Id, long.MaxValue));
            Assert.Empty(_db.ChangeTracker.Entries());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task CanSendMessage_DirectWithInvalidMemberCount_ReturnsFalse(int memberCount)
        {
            var alice = await CreateUserAsync("alice");
            var bob = await CreateUserAsync("bob");
            var direct = await CreateDirectConversationAsync(alice, bob, MemberStatus.Joined);
            if (memberCount == 1)
            {
                _db.ConversationMembers.Remove(await _db.ConversationMembers.SingleAsync(m => m.ConversationId == direct.Id && m.UserId == bob.Id));
            }
            else
            {
                var third = await CreateUserAsync("third");
                _db.ConversationMembers.Add(new ConversationMember { ConversationId = direct.Id, UserId = third.Id, Status = MemberStatus.Joined });
            }
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            Assert.False(await _queryService.CanSendMessageAsync(alice.Id, direct.Id));
        }

        [Fact]
        public async Task TransferAdmin_PersistsBothRolesInOneSaveChanges()
        {
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var group = await CreateGroupConversationAsync(owner);
            await _commandService.AddMemberAsync(new AddMemberReqDto(group.Id, member.Id, Status: MemberStatus.Joined));
            var saveCount = 0;
            _db.SavingChanges += (_, _) => saveCount++;

            var result = await _commandService.TransferAdminAsync(group.Id, owner.Id, member.Id);

            Assert.False(result.IsError);
            Assert.Equal(1, saveCount);
            _db.ChangeTracker.Clear();
            Assert.Equal(RoleType.Member, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == group.Id && m.UserId == owner.Id)).Role);
            Assert.Equal(RoleType.Admin, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == group.Id && m.UserId == member.Id)).Role);
        }

        [Fact]
        public async Task TransferAdmin_WhenPromotionFails_RollsBackBothRoles()
        {
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var group = await CreateGroupConversationAsync(owner);
            await _commandService.AddMemberAsync(new AddMemberReqDto(group.Id, member.Id, Status: MemberStatus.Joined));
            await _db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER FailAdminPromotion
                BEFORE UPDATE OF Role ON ConversationMembers
                WHEN NEW.Role = 1 AND OLD.Role = 0
                BEGIN
                    SELECT RAISE(ABORT, 'Simulated promotion failure');
                END;
                """);

            await Assert.ThrowsAsync<DbUpdateException>(() => _commandService.TransferAdminAsync(group.Id, owner.Id, member.Id));

            _db.ChangeTracker.Clear();
            Assert.Equal(RoleType.Admin, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == group.Id && m.UserId == owner.Id)).Role);
            Assert.Equal(RoleType.Member, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == group.Id && m.UserId == member.Id)).Role);
        }

        [Fact]
        public async Task TransferAdmin_WhenTargetPending_DoesNotDemoteCurrentAdmin()
        {
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var group = await CreateGroupConversationAsync(owner);
            await _commandService.AddMemberAsync(new AddMemberReqDto(group.Id, member.Id));

            var result = await _commandService.TransferAdminAsync(group.Id, owner.Id, member.Id);

            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
            _db.ChangeTracker.Clear();
            Assert.Equal(RoleType.Admin, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == group.Id && m.UserId == owner.Id)).Role);
            Assert.Equal(RoleType.Member, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == group.Id && m.UserId == member.Id)).Role);
        }

        [Fact]
        public async Task TransferAdmin_ToSelf_IsIdempotent()
        {
            var owner = await CreateUserAsync("owner");
            var group = await CreateGroupConversationAsync(owner);

            var result = await _commandService.TransferAdminAsync(group.Id, owner.Id, owner.Id);

            Assert.False(result.IsError);
            _db.ChangeTracker.Clear();
            Assert.Equal(RoleType.Admin, (await _db.ConversationMembers.SingleAsync(m => m.ConversationId == group.Id && m.UserId == owner.Id)).Role);
        }

        [Fact]
        public async Task DeleteConversation_RemovesConversationFromDb()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var group = await CreateGroupConversationAsync(owner);

            // Act
            var deleteResult = await _commandService.DeleteConversationAsync(group.Id);

            // Assert
            Assert.False(deleteResult.IsError);
            var exists = await _db.Conversations.AnyAsync(c => c.Id == group.Id);
            Assert.False(exists);
        }
    }
}

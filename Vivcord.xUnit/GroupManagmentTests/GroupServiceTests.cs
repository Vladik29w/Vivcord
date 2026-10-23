using ErrorOr;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vivcord.Server.DbContext;
using Vivcord.Server.Models;
using Vivcord.Server.Services;

namespace Vivcord.xUnit.GroupManagmentTests
{
    public class GroupServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly MainDbContext _db;
        private readonly IConversationCommandService _commandService;
        private readonly IConversationQueryService _queryService;
        private readonly IGroupService _groupService;

        public GroupServiceTests()
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
            _groupService = new GroupService(_db, _commandService, _queryService);
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

        [Fact]
        public async Task CreateGroup_CreatesGroupWithAdmin()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");

            // Act
            var result = await _groupService.CreateGroupAsync(owner.Id, "Developers");

            // Assert
            Assert.False(result.IsError);
            Assert.Equal("Developers", result.Value.Title);
            Assert.Equal("Group", result.Value.Type);

            var group = await _db.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == result.Value.Id);
            Assert.NotNull(group);
            var member = Assert.Single(group.Members);
            Assert.Equal(owner.Id, member.UserId);
            Assert.Equal(RoleType.Admin, member.Role);
            Assert.Equal(MemberStatus.Joined, member.Status);
        }

        [Fact]
        public async Task InviteMember_AddsPendingMember()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var groupResult = await _groupService.CreateGroupAsync(owner.Id, "Developers");

            // Act
            var inviteResult = await _groupService.InviteMemberAsync(owner.Id, groupResult.Value.Id, "member");

            // Assert
            Assert.False(inviteResult.IsError);

            var group = await _db.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == groupResult.Value.Id);
            Assert.NotNull(group);
            Assert.Equal(2, group.Members.Count);

            var pendingMember = group.Members.First(m => m.UserId == member.Id);
            Assert.Equal(MemberStatus.Pending, pendingMember.Status);
            Assert.Equal(owner.Id, pendingMember.InvitedById);
        }

        [Fact]
        public async Task AcceptInvite_SetsMemberToJoined()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var groupResult = await _groupService.CreateGroupAsync(owner.Id, "Developers");
            await _groupService.InviteMemberAsync(owner.Id, groupResult.Value.Id, "member");

            // Act
            var acceptResult = await _groupService.AcceptInviteAsync(member.Id, groupResult.Value.Id);

            // Assert
            Assert.False(acceptResult.IsError);

            var group = await _db.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == groupResult.Value.Id);
            var acceptedMember = group!.Members.First(m => m.UserId == member.Id);
            Assert.Equal(MemberStatus.Joined, acceptedMember.Status);
            Assert.NotNull(acceptedMember.JoinedAt);
        }

        [Fact]
        public async Task DeclineInvite_RemovesPendingMember()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var groupResult = await _groupService.CreateGroupAsync(owner.Id, "Developers");
            await _groupService.InviteMemberAsync(owner.Id, groupResult.Value.Id, "member");

            // Act
            var declineResult = await _groupService.DeclineInviteAsync(member.Id, groupResult.Value.Id);

            // Assert
            Assert.False(declineResult.IsError);

            var group = await _db.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == groupResult.Value.Id);
            Assert.Single(group!.Members);
            Assert.DoesNotContain(group.Members, m => m.UserId == member.Id);
        }

        [Fact]
        public async Task KickMember_AdminKicksMember_RemovesMember()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var groupResult = await _groupService.CreateGroupAsync(owner.Id, "Developers");
            await _groupService.InviteMemberAsync(owner.Id, groupResult.Value.Id, "member");
            await _groupService.AcceptInviteAsync(member.Id, groupResult.Value.Id);

            // Act
            var kickResult = await _groupService.KickMemberAsync(owner.Id, groupResult.Value.Id, "member");

            // Assert
            Assert.False(kickResult.IsError);

            var group = await _db.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == groupResult.Value.Id);
            Assert.Single(group!.Members);
            Assert.DoesNotContain(group.Members, m => m.UserId == member.Id);
        }

        [Fact]
        public async Task AssignAdmin_PromotesMember()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var groupResult = await _groupService.CreateGroupAsync(owner.Id, "Developers");
            await _groupService.InviteMemberAsync(owner.Id, groupResult.Value.Id, "member");
            await _groupService.AcceptInviteAsync(member.Id, groupResult.Value.Id);

            // Act
            var assignResult = await _groupService.AssignAdminAsync(owner.Id, groupResult.Value.Id, "member");

            // Assert
            Assert.False(assignResult.IsError);

            var group = await _db.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == groupResult.Value.Id);
            var newAdmin = group!.Members.First(m => m.UserId == member.Id);
            Assert.Equal(RoleType.Admin, newAdmin.Role);

            var oldAdmin = group.Members.First(m => m.UserId == owner.Id);
            Assert.Equal(RoleType.Member, oldAdmin.Role);
        }

        [Fact]
        public async Task AssignAdmin_WhenCommandReturnsError_PropagatesError()
        {
            var owner = await CreateUserAsync("owner");
            var member = await CreateUserAsync("member");
            var group = await _groupService.CreateGroupAsync(owner.Id, "Developers");
            await _groupService.InviteMemberAsync(owner.Id, group.Value.Id, "member");
            await _groupService.AcceptInviteAsync(member.Id, group.Value.Id);
            var commandService = new Mock<IConversationCommandService>(MockBehavior.Strict);
            var error = Error.Conflict("Group.TargetNotMember", "User must be an active member of the group.");
            commandService
                .Setup(c => c.TransferAdminAsync(group.Value.Id, owner.Id, member.Id, CancellationToken.None))
                .ReturnsAsync((ErrorOr<Success>)error);
            var service = new GroupService(_db, commandService.Object, _queryService);

            var result = await service.AssignAdminAsync(owner.Id, group.Value.Id, "member");

            Assert.True(result.IsError);
            Assert.Equal(error, result.FirstError);
            commandService.Verify(c => c.TransferAdminAsync(group.Value.Id, owner.Id, member.Id, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task DeleteGroup_AdminDeletes_RemovesGroup()
        {
            // Arrange
            var owner = await CreateUserAsync("owner");
            var groupResult = await _groupService.CreateGroupAsync(owner.Id, "Developers");

            // Act
            var deleteResult = await _groupService.DeleteGroupAsync(owner.Id, groupResult.Value.Id);

            // Assert
            Assert.False(deleteResult.IsError);
            var groupExists = await _db.Conversations.AnyAsync(c => c.Id == groupResult.Value.Id);
            Assert.False(groupExists);
        }

        [Fact]
        public async Task LeaveGroup_WhenLastAdmin_TransfersAdminOrDeletes()
        {
            // Arrange
            var admin1 = await CreateUserAsync("admin1");
            var member1 = await CreateUserAsync("member1");
            var groupResult = await _groupService.CreateGroupAsync(admin1.Id, "Developers");
            await _groupService.InviteMemberAsync(admin1.Id, groupResult.Value.Id, "member1");
            await _groupService.AcceptInviteAsync(member1.Id, groupResult.Value.Id);

            // Act: admin1 leaves
            var leaveResult = await _groupService.LeaveGroupAsync(admin1.Id, groupResult.Value.Id);

            // Assert
            Assert.False(leaveResult.IsError);

            var group = await _db.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == groupResult.Value.Id);
            Assert.NotNull(group);
            var remainingMember = Assert.Single(group.Members);
            Assert.Equal(member1.Id, remainingMember.UserId);
            Assert.Equal(RoleType.Admin, remainingMember.Role); // member1 automatically promoted to admin!
        }

        [Fact]
        public async Task GetUserGroups_ReturnsOnlyActiveGroupsOfUser()
        {
            // Arrange
            var user1 = await CreateUserAsync("user1");
            var user2 = await CreateUserAsync("user2");

            var group1 = await _groupService.CreateGroupAsync(user1.Id, "Group1");
            var group2 = await _groupService.CreateGroupAsync(user2.Id, "Group2");

            // user1 is invited to group2 but hasn't accepted yet
            await _groupService.InviteMemberAsync(user2.Id, group2.Value.Id, "user1");

            // Act
            var user1Groups = await _groupService.GetUserGroupsAsync(user1.Id);

            // Assert
            Assert.False(user1Groups.IsError);
            var singleGroup = Assert.Single(user1Groups.Value);
            Assert.Equal(group1.Value.Id, singleGroup.Id);
        }

        [Fact]
        public async Task InviteMember_WhenNonAdmin_ReturnsUnauthorized()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var member = await CreateUserAsync("member");
            var outsider = await CreateUserAsync("outsider");

            var group = await _groupService.CreateGroupAsync(admin.Id, "Team");
            await _groupService.InviteMemberAsync(admin.Id, group.Value.Id, "member");
            await _groupService.AcceptInviteAsync(member.Id, group.Value.Id);

            // Act: normal member tries to invite outsider
            var inviteResult = await _groupService.InviteMemberAsync(member.Id, group.Value.Id, "outsider");

            // Assert
            Assert.True(inviteResult.IsError);
            Assert.Equal(ErrorType.Unauthorized, inviteResult.FirstError.Type);
        }
    }
}

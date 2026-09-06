using ErrorOr;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;
using Vivcord.Server.Services;
using Xunit;

namespace Vivcord.xUnit.GroupManagmentTests
{
    public class GroupChatServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly MainDbContext _db;

        public GroupChatServiceTests()
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

        private async Task<AppUser> CreateUserAsync(string username)
        {
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = username,
                NormalizedUserName = username.ToUpperInvariant(),
                Email = $"{username}@test.com",
                NormalizedEmail = $"{username.ToUpperInvariant()}@TEST.COM"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        private async Task<GroupChat> CreateGroupAsync(Guid adminId, string name = "Test Group")
        {
            var group = new GroupChat
            {
                name = name,
                adminId = adminId
            };
            _db.GroupChats.Add(group);
            await _db.SaveChangesAsync();
            return group;
        }

        #region CreateGroupAsync Tests

        [Fact]
        public async Task CreateGroupAsync_UserNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var service = new GroupChatService(_db);
            var dto = new CreateGroupChatDTO("Test Group");

            // Act
            var result = await service.CreateGroupAsync(Guid.NewGuid(), dto);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("User not found", result.FirstError.Description);
        }

        [Fact]
        public async Task CreateGroupAsync_ValidUser_CreatesGroupAndReturnsGroupChatDTO()
        {
            // Arrange
            var user = await CreateUserAsync("owner");
            var service = new GroupChatService(_db);
            var dto = new CreateGroupChatDTO("Developers");

            // Act
            var result = await service.CreateGroupAsync(user.Id, dto);

            // Assert
            Assert.False(result.IsError);
            Assert.Equal("Developers", result.Value.Name);
            Assert.Equal(user.Id, result.Value.AdminId);
            Assert.Contains(user.Id, result.Value.MemberIds);
            Assert.NotEqual(Guid.Empty, result.Value.VoiceRoomId);

            var savedGroup = await _db.GroupChats.Include(g => g.Members).FirstOrDefaultAsync(g => g.id == result.Value.Id);
            Assert.NotNull(savedGroup);
            Assert.Equal("Developers", savedGroup.name);
            Assert.Single(savedGroup.Members);
            Assert.Equal(user.Id, savedGroup.Members.First().UserId);
        }

        #endregion

        #region DeleteGroupAsync Tests

        [Fact]
        public async Task DeleteGroupAsync_GroupNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var service = new GroupChatService(_db);

            // Act
            var result = await service.DeleteGroupAsync(Guid.NewGuid(), 999);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("Group not found", result.FirstError.Description);
        }

        [Fact]
        public async Task DeleteGroupAsync_UserNotAdmin_ReturnsUnauthorizedError()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var otherUser = await CreateUserAsync("other");
            var group = await CreateGroupAsync(admin.Id, "General");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.DeleteGroupAsync(otherUser.Id, group.id);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
            Assert.Equal("Only group admin can delete the group", result.FirstError.Description);
        }

        [Fact]
        public async Task DeleteGroupAsync_AdminUser_DeletesGroupAndReturnsSuccess()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var group = await CreateGroupAsync(admin.Id, "General");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.DeleteGroupAsync(admin.Id, group.id);

            // Assert
            Assert.False(result.IsError);
            Assert.Equal(Result.Success, result.Value);

            var deletedGroup = await _db.GroupChats.FirstOrDefaultAsync(g => g.id == group.id);
            Assert.Null(deletedGroup);
        }

        #endregion

        #region AddMemberAsync Tests

        [Fact]
        public async Task AddMemberAsync_GroupNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var service = new GroupChatService(_db);

            // Act
            var result = await service.AddMemberAsync(Guid.NewGuid(), 999, "someone");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("Group not found", result.FirstError.Description);
        }

        [Fact]
        public async Task AddMemberAsync_UserNotAdmin_ReturnsUnauthorizedError()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var nonAdmin = await CreateUserAsync("nonadmin");
            var group = await CreateGroupAsync(admin.Id, "Group 1");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.AddMemberAsync(nonAdmin.Id, group.id, "someone");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
            Assert.Equal("Only group admin can add members", result.FirstError.Description);
        }

        [Fact]
        public async Task AddMemberAsync_TargetUserNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var group = await CreateGroupAsync(admin.Id, "Group 1");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.AddMemberAsync(admin.Id, group.id, "ghost_user");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("User 'ghost_user' not found", result.FirstError.Description);
        }

        [Fact]
        public async Task AddMemberAsync_TargetUserAlreadyMember_ReturnsConflictError()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var targetUser = await CreateUserAsync("member1");
            var group = await CreateGroupAsync(admin.Id, "Group 1");

            _db.GroupChatMembers.Add(new GroupChatMember { GroupChatId = group.id, UserId = targetUser.Id });
            await _db.SaveChangesAsync();

            var service = new GroupChatService(_db);

            // Act
            var result = await service.AddMemberAsync(admin.Id, group.id, "member1");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
            Assert.Equal("User is already a member of this group", result.FirstError.Description);
        }

        [Fact]
        public async Task AddMemberAsync_ValidAdminAndTargetUser_AddsMemberAndReturnsSuccess()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var targetUser = await CreateUserAsync("new_member");
            var group = await CreateGroupAsync(admin.Id, "Group 1");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.AddMemberAsync(admin.Id, group.id, "new_member");

            // Assert
            Assert.False(result.IsError);
            Assert.Equal(Result.Success, result.Value);

            var memberExists = await _db.GroupChatMembers
                .AnyAsync(m => m.GroupChatId == group.id && m.UserId == targetUser.Id);
            Assert.True(memberExists);
        }

        #endregion

        #region RemoveMemberAsync Tests

        [Fact]
        public async Task RemoveMemberAsync_GroupNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var service = new GroupChatService(_db);

            // Act
            var result = await service.RemoveMemberAsync(Guid.NewGuid(), 999, "someone");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("Group not found", result.FirstError.Description);
        }

        [Fact]
        public async Task RemoveMemberAsync_UserNotAdmin_ReturnsUnauthorizedError()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var nonAdmin = await CreateUserAsync("nonadmin");
            var group = await CreateGroupAsync(admin.Id, "Group 1");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.RemoveMemberAsync(nonAdmin.Id, group.id, "someone");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
            Assert.Equal("Only group admin can remove members", result.FirstError.Description);
        }

        [Fact]
        public async Task RemoveMemberAsync_TargetUserNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var group = await CreateGroupAsync(admin.Id, "Group 1");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.RemoveMemberAsync(admin.Id, group.id, "unknown_user");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("User 'unknown_user' not found", result.FirstError.Description);
        }

        [Fact]
        public async Task RemoveMemberAsync_TargetUserNotMember_ReturnsNotFoundError()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var nonMemberUser = await CreateUserAsync("outsider");
            var group = await CreateGroupAsync(admin.Id, "Group 1");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.RemoveMemberAsync(admin.Id, group.id, "outsider");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("User is not a member of this group", result.FirstError.Description);
        }

        [Fact]
        public async Task RemoveMemberAsync_ValidMember_RemovesMemberAndReturnsSuccess()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var memberUser = await CreateUserAsync("member_to_remove");
            var group = await CreateGroupAsync(admin.Id, "Group 1");

            _db.GroupChatMembers.Add(new GroupChatMember { GroupChatId = group.id, UserId = memberUser.Id });
            await _db.SaveChangesAsync();

            var service = new GroupChatService(_db);

            // Act
            var result = await service.RemoveMemberAsync(admin.Id, group.id, "member_to_remove");

            // Assert
            Assert.False(result.IsError);
            Assert.Equal(Result.Success, result.Value);

            var memberExists = await _db.GroupChatMembers
                .AnyAsync(m => m.GroupChatId == group.id && m.UserId == memberUser.Id);
            Assert.False(memberExists);
        }

        #endregion

        #region AssignAdminAsync Tests

        [Fact]
        public async Task AssignAdminAsync_GroupNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var service = new GroupChatService(_db);

            // Act
            var result = await service.AssignAdminAsync(Guid.NewGuid(), 999, "newadmin");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("Group not found", result.FirstError.Description);
        }

        [Fact]
        public async Task AssignAdminAsync_UserNotAdmin_ReturnsUnauthorizedError()
        {
            // Arrange
            var currentAdmin = await CreateUserAsync("current_admin");
            var nonAdmin = await CreateUserAsync("non_admin");
            var group = await CreateGroupAsync(currentAdmin.Id, "Team Alpha");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.AssignAdminAsync(nonAdmin.Id, group.id, "newadmin");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
            Assert.Equal("Only current group admin can assign new admin", result.FirstError.Description);
        }

        [Fact]
        public async Task AssignAdminAsync_NewAdminUserNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var currentAdmin = await CreateUserAsync("current_admin");
            var group = await CreateGroupAsync(currentAdmin.Id, "Team Alpha");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.AssignAdminAsync(currentAdmin.Id, group.id, "nonexistent");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("User 'nonexistent' not found", result.FirstError.Description);
        }

        [Fact]
        public async Task AssignAdminAsync_NewAdminNotMember_ReturnsConflictError()
        {
            // Arrange
            var currentAdmin = await CreateUserAsync("current_admin");
            var candidateUser = await CreateUserAsync("candidate");
            var group = await CreateGroupAsync(currentAdmin.Id, "Team Alpha");

            var service = new GroupChatService(_db);

            // Act
            var result = await service.AssignAdminAsync(currentAdmin.Id, group.id, "candidate");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
            Assert.Equal("User must be a member of the group before becoming admin", result.FirstError.Description);
        }

        [Fact]
        public async Task AssignAdminAsync_ValidMember_UpdatesAdminIdAndReturnsSuccess()
        {
            // Arrange
            var currentAdmin = await CreateUserAsync("current_admin");
            var newAdminUser = await CreateUserAsync("promoted_admin");
            var group = await CreateGroupAsync(currentAdmin.Id, "Team Alpha");

            _db.GroupChatMembers.Add(new GroupChatMember { GroupChatId = group.id, UserId = newAdminUser.Id });
            await _db.SaveChangesAsync();

            var service = new GroupChatService(_db);

            // Act
            var result = await service.AssignAdminAsync(currentAdmin.Id, group.id, "promoted_admin");

            // Assert
            Assert.False(result.IsError);
            Assert.Equal(Result.Success, result.Value);

            _db.ChangeTracker.Clear();
            var updatedGroup = await _db.GroupChats.AsNoTracking().FirstOrDefaultAsync(g => g.id == group.id);
            Assert.NotNull(updatedGroup);
            Assert.Equal(newAdminUser.Id, updatedGroup.adminId);
        }

        #endregion

        #region GetGroupAsync Tests

        [Fact]
        public async Task GetGroupAsync_GroupNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var service = new GroupChatService(_db);

            // Act
            var result = await service.GetGroupAsync(999);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Equal("Group not found", result.FirstError.Description);
        }

        [Fact]
        public async Task GetGroupAsync_GroupHasVoiceRoomId_ReturnsDTO()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");
            var voiceRoomId = Guid.NewGuid();

            var group = new GroupChat
            {
                name = "Voice Chat Group",
                adminId = admin.Id,
                VoiceRoomId = voiceRoomId
            };
            _db.GroupChats.Add(group);
            await _db.SaveChangesAsync();

            _db.GroupChatMembers.Add(new GroupChatMember { GroupChatId = group.id, UserId = admin.Id });
            await _db.SaveChangesAsync();

            var service = new GroupChatService(_db);

            // Act
            var result = await service.GetGroupAsync(group.id);

            // Assert
            Assert.False(result.IsError);
            Assert.Equal(group.id, result.Value.Id);
            Assert.Equal("Voice Chat Group", result.Value.Name);
            Assert.Equal(admin.Id, result.Value.AdminId);
            Assert.Equal(voiceRoomId, result.Value.VoiceRoomId);
            Assert.Contains(admin.Id, result.Value.MemberIds);
            Assert.NotNull(result.Value.Members);
            Assert.Single(result.Value.Members);
            Assert.Equal(admin.Id, result.Value.Members[0].UserId);
            Assert.Equal("admin", result.Value.Members[0].UserName);
        }

        [Fact]
        public async Task GetGroupAsync_GroupHasEmptyVoiceRoomId_GeneratesNewVoiceRoomId()
        {
            // Arrange
            var admin = await CreateUserAsync("admin");

            var group = new GroupChat
            {
                name = "Legacy Group",
                adminId = admin.Id,
                VoiceRoomId = Guid.Empty
            };
            _db.GroupChats.Add(group);
            await _db.SaveChangesAsync();

            var service = new GroupChatService(_db);

            // Act
            var result = await service.GetGroupAsync(group.id);

            // Assert
            Assert.False(result.IsError);
            Assert.NotEqual(Guid.Empty, result.Value.VoiceRoomId);

            _db.ChangeTracker.Clear();
            var updatedGroup = await _db.GroupChats.AsNoTracking().FirstOrDefaultAsync(g => g.id == group.id);
            Assert.NotNull(updatedGroup);
            Assert.Equal(result.Value.VoiceRoomId, updatedGroup.VoiceRoomId);
        }

        #endregion

        #region GetUserGroupsAsync Tests

        [Fact]
        public async Task GetUserGroupsAsync_UserHasNoGroups_ReturnsEmptyList()
        {
            // Arrange
            var service = new GroupChatService(_db);

            // Act
            var result = await service.GetUserGroupsAsync(Guid.NewGuid());

            // Assert
            Assert.False(result.IsError);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetUserGroupsAsync_UserInGroups_ReturnsUserGroups()
        {
            // Arrange
            var user = await CreateUserAsync("user");
            var otherUser = await CreateUserAsync("other_user");

            var group1 = await CreateGroupAsync(user.Id, "Group 1");
            var group2 = await CreateGroupAsync(otherUser.Id, "Group 2");
            var group3 = await CreateGroupAsync(otherUser.Id, "Group 3");

            _db.GroupChatMembers.AddRange(
                new GroupChatMember { GroupChatId = group1.id, UserId = user.Id },
                new GroupChatMember { GroupChatId = group2.id, UserId = user.Id },
                new GroupChatMember { GroupChatId = group3.id, UserId = otherUser.Id }
            );
            await _db.SaveChangesAsync();

            var service = new GroupChatService(_db);

            // Act
            var result = await service.GetUserGroupsAsync(user.Id);

            // Assert
            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.Count);
            Assert.Contains(result.Value, g => g.Id == group1.id);
            Assert.Contains(result.Value, g => g.Id == group2.id);
            Assert.DoesNotContain(result.Value, g => g.Id == group3.id);
        }

        #endregion
    }
}

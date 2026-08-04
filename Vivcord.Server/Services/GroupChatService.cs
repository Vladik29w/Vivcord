using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services
{
    public interface IGroupChatService
    {
        Task<ErrorOr<GroupChatDTO>> CreateGroupAsync(Guid userId, CreateGroupChatDTO createGroupDto, CancellationToken cancellationToken = default);
        Task<ErrorOr<Success>> DeleteGroupAsync(Guid userId, int groupId, CancellationToken cancellationToken = default);
        Task<ErrorOr<Success>> AddMemberAsync(Guid userId, int groupId, string userNameToAdd, CancellationToken cancellationToken = default);
        Task<ErrorOr<Success>> RemoveMemberAsync(Guid userId, int groupId, string userNameToRemove, CancellationToken cancellationToken = default);
        Task<ErrorOr<Success>> AssignAdminAsync(Guid userId, int groupId, string userNameToMakeAdmin, CancellationToken cancellationToken = default);
        Task<ErrorOr<GroupChatDTO>> GetGroupAsync(int groupId, CancellationToken cancellationToken = default);
        Task<ErrorOr<IReadOnlyList<GroupChatDTO>>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    public class GroupChatService(MainDbContext dbContext) : IGroupChatService
    {
        public async Task<ErrorOr<GroupChatDTO>> CreateGroupAsync(Guid userId, CreateGroupChatDTO createGroupDto, CancellationToken cancellationToken = default)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return Error.NotFound(description: "User not found");

            var newGroup = new GroupChat
            {
                name = createGroupDto.Name,
                adminId = userId
            };

            dbContext.GroupChats.Add(newGroup);
            await dbContext.SaveChangesAsync(cancellationToken);

            //creator is a member
            var membership = new GroupChatMember
            {
                GroupChatId = newGroup.id,
                UserId = userId
            };
            dbContext.GroupChatMembers.Add(membership);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new GroupChatDTO(newGroup.id, newGroup.name, newGroup.adminId, new[] { userId });
        }

        public async Task<ErrorOr<Success>> DeleteGroupAsync(Guid userId, int groupId, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats.FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);
            if (group == null)
                return Error.NotFound(description: "Group not found");

            if (group.adminId != userId)
                return Error.Unauthorized(description: "Only group admin can delete the group");

            dbContext.GroupChats.Remove(group);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }

        public async Task<ErrorOr<Success>> AddMemberAsync(Guid userId, int groupId, string userNameToAdd, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats.FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);
            if (group == null)
                return Error.NotFound(description: "Group not found");

            if (group.adminId != userId)
                return Error.Unauthorized(description: "Only group admin can add members");

            var userToAdd = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userNameToAdd, cancellationToken);
            if (userToAdd == null)
                return Error.NotFound(description: $"User {userNameToAdd} not found");

            var alreadyMember = await dbContext.GroupChatMembers
                .AnyAsync(gcm => gcm.GroupChatId == groupId && gcm.UserId == userToAdd.Id, cancellationToken);

            if (alreadyMember)
                return Error.Conflict(description: "User is already a member of this group");

            var membership = new GroupChatMember
            {
                GroupChatId = groupId,
                UserId = userToAdd.Id
            };

            dbContext.GroupChatMembers.Add(membership);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }

        public async Task<ErrorOr<Success>> RemoveMemberAsync(Guid userId, int groupId, string userNameToRemove, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats.FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);
            if (group == null)
                return Error.NotFound(description: "Group not found");

            if (group.adminId != userId)
                return Error.Unauthorized(description: "Only group admin can remove members");

            var userToRemove = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userNameToRemove, cancellationToken);
            if (userToRemove == null)
                return Error.NotFound(description: $"User {userNameToRemove} not found");

            var membership = await dbContext.GroupChatMembers
                .FirstOrDefaultAsync(gcm => gcm.GroupChatId == groupId && gcm.UserId == userToRemove.Id, cancellationToken);

            if (membership == null)
                return Error.NotFound(description: "User is not a member of this group");

            dbContext.GroupChatMembers.Remove(membership);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }

        public async Task<ErrorOr<Success>> AssignAdminAsync(Guid userId, int groupId, string userNameToMakeAdmin, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats.FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);
            if (group == null)
                return Error.NotFound(description: "Group not found");

            if (group.adminId != userId)
                return Error.Unauthorized(description: "Only current group admin can assign new admin");

            var userToMakeAdmin = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userNameToMakeAdmin, cancellationToken);
            if (userToMakeAdmin == null)
                return Error.NotFound(description: $"User {userNameToMakeAdmin} not found");

            var isMember = await dbContext.GroupChatMembers
                .AnyAsync(gcm => gcm.GroupChatId == groupId && gcm.UserId == userToMakeAdmin.Id, cancellationToken);

            if (!isMember)
                return Error.Conflict(description: "User must be a member of the group before becoming admin");

            group.adminId = userToMakeAdmin.Id;
            dbContext.GroupChats.Update(group);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }

        public async Task<ErrorOr<GroupChatDTO>> GetGroupAsync(int groupId, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);

            if (group == null)
                return Error.NotFound(description: "Group not found");

            var memberIds = await dbContext.GroupChatMembers
                .AsNoTracking()
                .Where(gcm => gcm.GroupChatId == groupId)
                .Select(gcm => gcm.UserId)
                .ToListAsync(cancellationToken);

            return new GroupChatDTO(group.id, group.name, group.adminId, memberIds);
        }

        public async Task<ErrorOr<IReadOnlyList<GroupChatDTO>>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var groups = await dbContext.GroupChats
                .AsNoTracking()
                .Where(g => g.Members.Any(m => m.UserId == userId))
                .ToListAsync(cancellationToken);

            var result = new List<GroupChatDTO>();
            foreach (var group in groups)
            {
                var memberIds = await dbContext.GroupChatMembers
                    .AsNoTracking()
                    .Where(gcm => gcm.GroupChatId == group.id)
                    .Select(gcm => gcm.UserId)
                    .ToListAsync(cancellationToken);

                result.Add(new GroupChatDTO(group.id, group.name, group.adminId, memberIds));
            }

            return result.AsReadOnly();
        }
    }
}

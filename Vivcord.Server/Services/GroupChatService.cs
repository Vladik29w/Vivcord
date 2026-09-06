using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;

namespace Vivcord.Server.Services
{
    public interface IGroupChatService
    {
        Task<ErrorOr<GroupChatDTO>> CreateGroupAsync(Guid userId, CreateGroupChatDTO dto, CancellationToken cancellationToken = default);
        Task<ErrorOr<Success>> DeleteGroupAsync(Guid userId, int groupId, CancellationToken cancellationToken = default);
        Task<ErrorOr<Success>> AddMemberAsync(Guid userId, int groupId, string username, CancellationToken cancellationToken = default);
        Task<ErrorOr<Success>> RemoveMemberAsync(Guid userId, int groupId, string username, CancellationToken cancellationToken = default);
        Task<ErrorOr<Success>> AssignAdminAsync(Guid userId, int groupId, string newAdminUsername, CancellationToken cancellationToken = default);
        Task<ErrorOr<GroupChatDTO>> GetGroupAsync(int groupId, CancellationToken cancellationToken = default);
        Task<ErrorOr<IReadOnlyList<GroupChatDTO>>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    public class GroupChatService(MainDbContext dbContext) : IGroupChatService
    {
        public async Task<ErrorOr<GroupChatDTO>> CreateGroupAsync(Guid userId, CreateGroupChatDTO dto, CancellationToken cancellationToken = default)
        {
            var userExists = await dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
                return Error.NotFound(description: "User not found");

            var newGroup = new GroupChat
            {
                name = dto.Name,
                adminId = userId,
                VoiceRoomId = Guid.NewGuid(),
                Members = [new GroupChatMember { UserId = userId }]
            };

            dbContext.GroupChats.Add(newGroup);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new GroupChatDTO
            {
                Id = newGroup.id,
                Name = newGroup.name,
                AdminId = newGroup.adminId,
                MemberIds = [userId],
                VoiceRoomId = newGroup.VoiceRoomId
            };
        }

        public async Task<ErrorOr<Success>> DeleteGroupAsync(Guid userId, int groupId, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);

            if (group == null)
                return Error.NotFound(description: "Group not found");

            if (group.adminId != userId)
                return Error.Unauthorized(description: "Only group admin can delete the group");

            await dbContext.GroupChats
                .Where(g => g.id == groupId)
                .ExecuteDeleteAsync(cancellationToken);

            return Result.Success;
        }

        public async Task<ErrorOr<Success>> AddMemberAsync(Guid userId, int groupId, string username, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);

            if (group == null)
                return Error.NotFound(description: "Group not found");

            if (group.adminId != userId)
                return Error.Unauthorized(description: "Only group admin can add members");

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == username, cancellationToken);

            if (user == null)
                return Error.NotFound(description: $"User '{username}' not found");

            var isAlreadyMember = await dbContext.GroupChatMembers
                .AnyAsync(m => m.GroupChatId == groupId && m.UserId == user.Id, cancellationToken);

            if (isAlreadyMember)
                return Error.Conflict(description: "User is already a member of this group");

            var membership = new GroupChatMember
            {
                GroupChatId = groupId,
                UserId = user.Id
            };

            dbContext.GroupChatMembers.Add(membership);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }

        public async Task<ErrorOr<Success>> RemoveMemberAsync(Guid userId, int groupId, string username, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);

            if (group == null)
                return Error.NotFound(description: "Group not found");

            if (group.adminId != userId)
                return Error.Unauthorized(description: "Only group admin can remove members");

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == username, cancellationToken);

            if (user == null)
                return Error.NotFound(description: $"User '{username}' not found");

            var deletedRows = await dbContext.GroupChatMembers
                .Where(m => m.GroupChatId == groupId && m.UserId == user.Id)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedRows == 0)
                return Error.NotFound(description: "User is not a member of this group");

            return Result.Success;
        }

        public async Task<ErrorOr<Success>> AssignAdminAsync(Guid userId, int groupId, string newAdminUsername, CancellationToken cancellationToken = default)
        {
            var group = await dbContext.GroupChats
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.id == groupId, cancellationToken);

            if (group == null)
                return Error.NotFound(description: "Group not found");

            if (group.adminId != userId)
                return Error.Unauthorized(description: "Only current group admin can assign new admin");

            var newAdminUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == newAdminUsername, cancellationToken);

            if (newAdminUser == null)
                return Error.NotFound(description: $"User '{newAdminUsername}' not found");

            var isMember = await dbContext.GroupChatMembers
                .AnyAsync(m => m.GroupChatId == groupId && m.UserId == newAdminUser.Id, cancellationToken);

            if (!isMember)
                return Error.Conflict(description: "User must be a member of the group before becoming admin");

            await dbContext.GroupChats
                .Where(g => g.id == groupId)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.adminId, newAdminUser.Id), cancellationToken);

            return Result.Success;
        }

        public async Task<ErrorOr<GroupChatDTO>> GetGroupAsync(int groupId, CancellationToken cancellationToken = default)
        {
            var groupDto = await dbContext.GroupChats
                .AsNoTracking()
                .Where(g => g.id == groupId)
                .Select(g => new GroupChatDTO
                {
                    Id = g.id,
                    Name = g.name,
                    AdminId = g.adminId,
                    MemberIds = g.Members.Select(m => m.UserId).ToList(),
                    VoiceRoomId = g.VoiceRoomId,
                    Members = g.Members.Select(m => new UserProfileDTO(
                        m.UserId,
                        m.User.UserName ?? string.Empty,
                        m.User.DisplayName,
                        m.User.ProfilePictureUrl
                    )).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (groupDto == null)
                return Error.NotFound(description: "Group not found");

            if (groupDto.VoiceRoomId == Guid.Empty)
            {
                var newVoiceRoomId = Guid.NewGuid();
                await dbContext.GroupChats
                    .Where(g => g.id == groupId)
                    .ExecuteUpdateAsync(s => s.SetProperty(g => g.VoiceRoomId, newVoiceRoomId), cancellationToken);

                groupDto = groupDto with { VoiceRoomId = newVoiceRoomId };
            }

            return groupDto;
        }

        public async Task<ErrorOr<IReadOnlyList<GroupChatDTO>>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var groups = await dbContext.GroupChats
                .AsNoTracking()
                .Where(g => g.Members.Any(m => m.UserId == userId))
                .Select(g => new GroupChatDTO
                {
                    Id = g.id,
                    Name = g.name,
                    AdminId = g.adminId,
                    MemberIds = g.Members.Select(m => m.UserId).ToList(),
                    VoiceRoomId = g.VoiceRoomId
                })
                .ToListAsync(cancellationToken);

            return groups.AsReadOnly();
        }
    }
}


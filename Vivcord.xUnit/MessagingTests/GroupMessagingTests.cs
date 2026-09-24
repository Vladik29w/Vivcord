using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Models;
using Vivcord.Server.Services;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.xUnit.MessagingTests;

public class GroupMessagingTests
{
    private static MainDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new MainDbContext(options);
    }

    private static TimeProvider FrozenTime(DateTimeOffset frozen)
    {
        var mock = new Mock<TimeProvider>();
        mock.Setup(t => t.GetUtcNow()).Returns(frozen);
        return mock.Object;
    }

    private static IBlobStorageService NullBlobStorage()
    {
        // Attachment URLs in these tests are always null, so this mock is never invoked.
        return new Mock<IBlobStorageService>().Object;
    }

    private static MessageSendingService CreateService(MainDbContext db, TimeProvider? time = null)
        => new(db, time ?? TimeProvider.System, NullBlobStorage());

    private static async Task AddMemberAsync(MainDbContext db, int groupId, Guid userId)
    {
        db.GroupChatMembers.Add(new GroupChatMember { GroupChatId = groupId, UserId = userId });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SendGroupMessageAsync_Persists_Message_To_Database()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 1,
            Text = "Hello group!",
            AttachmentUrl = null,
            AttachmentType = null
        };
        await AddMemberAsync(db, dto.GroupId, dto.SenderId);

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert
        Assert.False(result.IsError);
        var saved = await db.GroupMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("Hello group!", saved.Text);
    }

    [Fact]
    public async Task SendGroupMessageAsync_WhenUserNotMember_ReturnsForbidden()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 1,
            Text = "Unauthorized message",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
        Assert.Equal(0, await db.GroupMessages.CountAsync());
    }

    [Fact]
    public async Task SendGroupMessageAsync_Correctly_Maps_SenderId_And_GroupId()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var senderGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        const int groupId = 42;

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = senderGuid,
            GroupId = groupId,
            Text = "Mapping test",
            AttachmentUrl = null,
            AttachmentType = null
        };
        await AddMemberAsync(db, groupId, senderGuid);

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert — returned result
        Assert.False(result.IsError);
        var saved = await db.GroupMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(senderGuid, saved.Sender);
        Assert.Equal(groupId, saved.GroupId);
        Assert.Equal(result.Value.Id, saved.id);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Uses_TimeProvider_For_SentAt()
    {
        // Arrange
        var frozenNow = new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero);

        await using var db = CreateDbContext();
        var service = CreateService(db, FrozenTime(frozenNow));

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 7,
            Text = "Timestamp test",
            AttachmentUrl = null,
            AttachmentType = null
        };
        await AddMemberAsync(db, dto.GroupId, dto.SenderId);

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert
        Assert.False(result.IsError);
        var saved = await db.GroupMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(frozenNow, saved.SentAt);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Supports_Null_Attachment()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 3,
            Text = "No attachment",
            AttachmentUrl = null,
            AttachmentType = null
        };
        await AddMemberAsync(db, dto.GroupId, dto.SenderId);

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert
        Assert.False(result.IsError);
        Assert.Null(result.Value.SasAttachmentUrl);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Supports_Image_Attachment()
    {
        // Arrange
        var blobMock = new Mock<IBlobStorageService>();
        blobMock
            .Setup(b => b.GenerateSasReadUrl(BlobContainers.ChatMedia, "images/photo.jpg"))
            .Returns("https://storage.example.com/images/photo.jpg?sas=token");

        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System, blobMock.Object);

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 5,
            Text = "With image",
            AttachmentUrl = "images/photo.jpg",
            AttachmentType = "image"
        };
        await AddMemberAsync(db, dto.GroupId, dto.SenderId);

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert — SAS URL was generated and returned
        Assert.False(result.IsError);
        Assert.NotNull(result.Value.SasAttachmentUrl);
        Assert.Contains("sas=token", result.Value.SasAttachmentUrl);

        // Assert — raw blob name is stored in DB, not the SAS URL
        var saved = await db.GroupMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("images/photo.jpg", saved.AttachmentUrl);
        Assert.Equal("image", saved.AttachmentType);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Each_Call_Creates_Separate_Row()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        const int groupId = 99;
        var senderA = Guid.NewGuid();
        var senderB = Guid.NewGuid();

        var dto1 = new GroupMessageDto
        {
            Id = 0,
            SenderId = senderA,
            GroupId = groupId,
            Text = "First message",
            AttachmentUrl = null,
            AttachmentType = null
        };
        var dto2 = new GroupMessageDto
        {
            Id = 0,
            SenderId = senderB,
            GroupId = groupId,
            Text = "Second message",
            AttachmentUrl = null,
            AttachmentType = null
        };
        await AddMemberAsync(db, groupId, senderA);
        await AddMemberAsync(db, groupId, senderB);

        // Act
        var res1 = await service.SendGroupMessageAsync(dto1);
        var res2 = await service.SendGroupMessageAsync(dto2);

        // Assert — two distinct rows were created
        Assert.False(res1.IsError);
        Assert.False(res2.IsError);
        var all = await db.GroupMessages.OrderBy(m => m.id).ToListAsync();
        Assert.Equal(2, all.Count);

        // Assert — each row contains the correct data, not overwritten by the next call
        Assert.Equal(senderA,         all[0].Sender);
        Assert.Equal(groupId,         all[0].GroupId);
        Assert.Equal("First message", all[0].Text);

        Assert.Equal(senderB,          all[1].Sender);
        Assert.Equal(groupId,          all[1].GroupId);
        Assert.Equal("Second message", all[1].Text);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Messages_For_Different_Groups_Are_Isolated()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var sender = Guid.NewGuid();
        var dto1 = new GroupMessageDto
        {
            Id = 0,
            SenderId = sender,
            GroupId = 1,
            Text = "Group 1 message",
            AttachmentUrl = null,
            AttachmentType = null
        };
        var dto2 = new GroupMessageDto
        {
            Id = 0,
            SenderId = sender,
            GroupId = 2,
            Text = "Group 2 message",
            AttachmentUrl = null,
            AttachmentType = null
        };
        await AddMemberAsync(db, 1, sender);
        await AddMemberAsync(db, 2, sender);

        // Act
        var res1 = await service.SendGroupMessageAsync(dto1);
        var res2 = await service.SendGroupMessageAsync(dto2);

        // Assert
        Assert.False(res1.IsError);
        Assert.False(res2.IsError);
        var group1Messages = await db.GroupMessages.Where(m => m.GroupId == 1).CountAsync();
        var group2Messages = await db.GroupMessages.Where(m => m.GroupId == 2).CountAsync();
        Assert.Equal(1, group1Messages);
        Assert.Equal(1, group2Messages);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Populates_SenderAvatarUrl_When_User_Exists()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var senderId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = senderId,
            UserName = "bob",
            DisplayName = "Bob",
            ProfilePictureUrl = "https://example.com/bob-avatar.png"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = senderId,
            SenderName = "Bob",
            GroupId = 10,
            Text = "Group message with avatar",
            AttachmentUrl = null,
            AttachmentType = null
        };
        await AddMemberAsync(db, dto.GroupId, senderId);

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("https://example.com/bob-avatar.png", result.Value.SenderAvatarUrl);
        Assert.Equal("Bob", result.Value.SenderName);
    }
}

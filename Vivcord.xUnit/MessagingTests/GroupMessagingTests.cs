using Microsoft.EntityFrameworkCore;
using Moq;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
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

    [Fact]
    public async Task SendGroupMessageAsync_Persists_Message_To_Database()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System);

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 1,
            Text = "Hello group!",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        await service.SendGroupMessageAsync(dto);

        // Assert
        var saved = await db.GroupMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("Hello group!", saved.Text);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Correctly_Maps_SenderId_And_GroupId()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System);

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

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert — returned object
        Assert.Equal(senderGuid, result.Sender);
        Assert.Equal(groupId, result.GroupId);

        // Assert — what was actually persisted in the DB
        var saved = await db.GroupMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(senderGuid, saved.Sender);
        Assert.Equal(groupId, saved.GroupId);
        Assert.Equal(result.id, saved.id);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Uses_TimeProvider_For_SentAt()
    {
        // Arrange
        var frozenNow = new DateTimeOffset(2025, 3, 10, 12, 0, 0, TimeSpan.Zero);

        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, FrozenTime(frozenNow));

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 7,
            Text = "Timestamp test",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert
        Assert.Equal(frozenNow, result.SentAt);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Supports_Null_Attachment()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System);

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 3,
            Text = "No attachment",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert
        Assert.Null(result.AttachmentUrl);
        Assert.Null(result.AttachmentType);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Supports_Image_Attachment()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System);

        var dto = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 5,
            Text = "With image",
            AttachmentUrl = "images/photo.jpg",
            AttachmentType = "image"
        };

        // Act
        var result = await service.SendGroupMessageAsync(dto);

        // Assert
        Assert.Equal("images/photo.jpg", result.AttachmentUrl);
        Assert.Equal("image", result.AttachmentType);
    }

    [Fact]
    public async Task SendGroupMessageAsync_Each_Call_Creates_Separate_Row()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System);

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

        // Act
        await service.SendGroupMessageAsync(dto1);
        await service.SendGroupMessageAsync(dto2);

        // Assert — two distinct rows were created
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
        var service = new MessageSendingService(db, TimeProvider.System);

        var dto1 = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 1,
            Text = "Group 1 message",
            AttachmentUrl = null,
            AttachmentType = null
        };
        var dto2 = new GroupMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            GroupId = 2,
            Text = "Group 2 message",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        await service.SendGroupMessageAsync(dto1);
        await service.SendGroupMessageAsync(dto2);

        // Assert
        var group1Messages = await db.GroupMessages.Where(m => m.GroupId == 1).CountAsync();
        var group2Messages = await db.GroupMessages.Where(m => m.GroupId == 2).CountAsync();
        Assert.Equal(1, group1Messages);
        Assert.Equal(1, group2Messages);
    }
}

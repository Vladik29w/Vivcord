using Microsoft.EntityFrameworkCore;
using Moq;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.xUnit.MessagingTests;

public class MessageSendingServiceTests
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
    public async Task SendPrivateMessageAsync_Persists_Message_To_Database()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System);

        var dto = new PrivateMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            TargetUserId = Guid.NewGuid(),
            Text = "Hello!",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        await service.SendPrivateMessageAsync(dto);

        // Assert
        var saved = await db.PrivateMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("Hello!", saved.Text);
    }

    [Fact]
    public async Task SendPrivateMessageAsync_Correctly_Maps_SenderId_And_TargetUserId()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System);

        var senderGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var targetGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var dto = new PrivateMessageDto
        {
            Id = 0,
            SenderId = senderGuid,
            TargetUserId = targetGuid,
            Text = "Valid Guids mapping test",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        var result = await service.SendPrivateMessageAsync(dto);

        // Assert — returned object
        Assert.Equal(senderGuid, result.Sender);
        Assert.Equal(targetGuid, result.Target);

        // Assert — what was actually persisted in the DB
        var saved = await db.PrivateMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(senderGuid, saved.Sender);
        Assert.Equal(targetGuid, saved.Target);
        Assert.Equal(result.id, saved.id);
    }

    [Fact]
    public async Task SendPrivateMessageAsync_Uses_TimeProvider_For_SentAt()
    {
        // Arrange
        var frozenNow = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);

        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, FrozenTime(frozenNow));

        var dto = new PrivateMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            TargetUserId = Guid.NewGuid(),
            Text = "Timestamp test",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        var result = await service.SendPrivateMessageAsync(dto);

        // Assert
        Assert.Equal(frozenNow, result.SentAt);
    }

    [Fact]
    public async Task SendPrivateMessageAsync_Supports_Null_Attachment()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System);

        var dto = new PrivateMessageDto
        {
            Id = 0,
            SenderId = Guid.NewGuid(),
            TargetUserId = Guid.NewGuid(),
            Text = "No attachment",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        var result = await service.SendPrivateMessageAsync(dto);

        // Assert
        Assert.Null(result.AttachmentUrl);
        Assert.Null(result.AttachmentType);
    }

    [Fact]
    public async Task SendPrivateMessageAsync_Each_Call_Creates_Separate_Row()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = new MessageSendingService(db, TimeProvider.System);

        var senderA = Guid.NewGuid();
        var senderB = Guid.NewGuid();
        var target  = Guid.NewGuid();

        var dto1 = new PrivateMessageDto
        {
            Id = 0,
            SenderId = senderA,
            TargetUserId = target,
            Text = "First",
            AttachmentUrl = null,
            AttachmentType = null
        };
        var dto2 = new PrivateMessageDto
        {
            Id = 0,
            SenderId = senderB,
            TargetUserId = target,
            Text = "Second",
            AttachmentUrl = null,
            AttachmentType = null
        };

        // Act
        await service.SendPrivateMessageAsync(dto1);
        await service.SendPrivateMessageAsync(dto2);

        // Assert — two distinct rows were created
        var all = await db.PrivateMessages.OrderBy(m => m.id).ToListAsync();
        Assert.Equal(2, all.Count);

        // Assert — each row contains the correct data, not overwritten by the next call
        Assert.Equal(senderA, all[0].Sender);
        Assert.Equal(target,  all[0].Target);
        Assert.Equal("First", all[0].Text);

        Assert.Equal(senderB, all[1].Sender);
        Assert.Equal(target,  all[1].Target);
        Assert.Equal("Second", all[1].Text);
    }
}

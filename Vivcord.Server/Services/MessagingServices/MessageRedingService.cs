using Microsoft.EntityFrameworkCore;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Services.MessagingServices
{
    public interface IMessagingService
    {
        Task<IReadOnlyList<MessageDto>> GetChatHistory(string currentUserId, string targetUserId, CancellationToken cancellationToken = default);
    }
    public class MessageRedingService(MainDbContext dbContext, IBlobStorageService blobStorageService) : IMessagingService
    {
        public async Task<IReadOnlyList<MessageDto>> GetChatHistory(string currentUserId, string targetUserId, CancellationToken cancellationToken = default)
        {
            var currentGuid = Guid.Parse(currentUserId);
            var targetGuid = Guid.Parse(targetUserId);

            var messages = await dbContext.UserMessages
                .Where(m => (m.Sender == currentGuid && m.Target == targetGuid) ||
                            (m.Sender == targetGuid && m.Target == currentGuid))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.id,
                    m.Sender,
                    m.Text,
                    m.AttachmentUrl,
                    m.AttachmentType,
                })
                .ToListAsync(cancellationToken);

            return messages.Select(m =>
            {
                string? sasReadUrl = null;
                if (m.AttachmentUrl is not null)
                {
                    var result = blobStorageService.GenerateSasReadUrl(m.AttachmentUrl);
                    sasReadUrl = result.IsError ? null : result.Value;
                }

                return new MessageDto
                {
                    Id = m.id,
                    SenderId = m.Sender,
                    TargetUserId = Guid.Empty,
                    Text = m.Text,
                    AttachmentUrl = sasReadUrl,
                    AttachmentType = m.AttachmentType
                };
            }).ToList();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.Controllers.Main;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Services;
using Vivcord.Server.Services.MessagingServices;

namespace Vivcord.Server.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class MessagingController(
        IMessageReadingService messagingService,
        MainDbContext dbContext) : ApiMainController
    {
        [HttpGet("history/{conversationId:long}")]
        public async Task<IActionResult> GetChatHistory(long conversationId, CancellationToken cancellationToken)
        {
            var currentUserId = CurrentUserId!.Value;
            var result = await messagingService.GetChatHistory(conversationId, currentUserId, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpGet("group-history/{groupId:long}")]
        public async Task<IActionResult> GetGroupChatHistory(long groupId, CancellationToken cancellationToken)
        {
            var currentUserId = CurrentUserId!.Value;
            var result = await messagingService.GetChatHistory(groupId, currentUserId, cancellationToken);
            return result.Match(Ok, Problem);
        }

        [HttpGet("history/{targetUserId:guid}")]
        public async Task<IActionResult> GetDirectChatHistory(Guid targetUserId, CancellationToken cancellationToken)
        {
            var currentUserId = CurrentUserId!.Value;
            var pairKey = ConversationService.GenerateDirectPairKey(currentUserId, targetUserId);
            var conversation = await dbContext.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.DirectPairKey == pairKey, cancellationToken);

            if (conversation == null)
            {
                return Ok(Array.Empty<MessageDto>());
            }

            var result = await messagingService.GetChatHistory(conversation.Id, currentUserId, cancellationToken);
            return result.Match(Ok, Problem);
        }
    }
}

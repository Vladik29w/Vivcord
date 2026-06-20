using Microsoft.AspNetCore.SignalR;

namespace Vivcord.Server.Infastructure.SignalR
{
    public class LowercaseUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var userId = connection.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userId?.ToLowerInvariant();
        }
    }
}

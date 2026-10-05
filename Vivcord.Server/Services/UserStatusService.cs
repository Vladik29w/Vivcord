using System.Collections.Concurrent;

namespace Vivcord.Server.Services
{
    public interface IUserStatusService
    {
        bool UserConnect(Guid userId, string connectionId);
        bool UserDisconnect(Guid userId, string connectionId);
        bool IsUserActive(Guid userId);
    }
    public class UserStatusService : IUserStatusService
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _userConnections = new();

        public bool UserConnect(Guid userId, string connectionId)
        {
            var connections = _userConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            connections.TryAdd(connectionId, 0);
            return connections.Count == 1;
        }

        public bool UserDisconnect(Guid userId, string connectionId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.TryRemove(connectionId, out _);
                if (connections.IsEmpty)
                {
                    _userConnections.TryRemove(KeyValuePair.Create(userId, connections));
                    return true;
                }
            }
            return false;
        }
        public bool IsUserActive(Guid userId)
        {
            return _userConnections.TryGetValue(userId, out var connections) && !connections.IsEmpty;
        }
    }
}

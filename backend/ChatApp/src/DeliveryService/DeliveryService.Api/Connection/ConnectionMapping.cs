
using System.Collections.Concurrent;

namespace DeliveryService.Api.Connection
{
    public class ConnectionMapping : IConnectionMapping
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _connections =
            new ConcurrentDictionary<string, HashSet<string>>();
        public void Add(string UserId, string ConnectionId)
        {
            _connections.AddOrUpdate(UserId,
                _ => new HashSet<string> { ConnectionId },
                (_, existingConnections) =>
                {
                    lock (existingConnections)
                    {
                        existingConnections.Add(ConnectionId);
                    }
                    return existingConnections;
                });
        }

        public IEnumerable<string> GetConnections(string UserId)
        {
            if (_connections.TryGetValue(UserId, out var connections))
            {
                
                return connections.ToList();
            }
            return Enumerable.Empty<string>();
        }

        public void Remove(string UserId, string ConnectionId)
        {
            if (_connections.TryGetValue(UserId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(ConnectionId);
                    if (connections.Count == 0)
                    {
                        _connections.TryRemove(UserId, out _);
                    }
                }
            }
        }
    }
}

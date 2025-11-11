namespace DeliveryService.Api.Connection
{
    public interface IConnectionMapping
    {
        void Add(string UserId, string ConnectionId);
        void Remove(string UserId, string ConnectionId);
        IEnumerable<string> GetConnections(string UserId);
    }
}

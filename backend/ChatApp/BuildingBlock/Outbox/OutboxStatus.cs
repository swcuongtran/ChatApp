namespace BuildingBlock.Outbox
{
    public enum OutboxStatus
    {
        Pending = 0,
        Dispatched = 1,
        Failed = 2
    }
}

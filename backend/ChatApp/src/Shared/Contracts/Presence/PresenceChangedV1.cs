namespace Contracts.Presence
{
    public enum PresenceStatus
    {
        Online,
        Away,
        Busy,
        Offline
    }
    public sealed record PresenceChangedV1(
        string UserId,
        PresenceStatus NewStatus,
        DateTimeOffset ChangedAtUtc
    );
}

namespace Contracts.Notification
{
    public enum NotificationChannel
    {
        Email,
        SMS,
        Push,
        InApp
    }
    public enum NotificationStatus
    {
        Sent,
        Delivered,
        Read,
        Failed
    }
    public sealed record NotificationDispatchedV1(
        string NotificationId,
        string UserId,
        NotificationChannel Channel,
        NotificationStatus Status,
        DateTimeOffset DispatchedAtUtc,
        string? FailureReason
    );
}

namespace FedCarrier.Notification.Domain;

public enum NotificationType
{
    ShipmentStatus = 0,
    DeliveryConfirmation = 1,
    Payment = 2,
    Promotional = 3,
    System = 4
}

public enum NotificationChannel
{
    Email = 0,
    Sms = 1,
    Push = 2
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Recipient { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? Error { get; set; }
}

public class NotificationTemplate
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

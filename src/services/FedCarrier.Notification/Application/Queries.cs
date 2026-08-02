using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Notification.Domain;

namespace FedCarrier.Notification.Application.Queries;

public class GetNotificationsQuery : IRequest<ApiResponse<PagedResult<NotificationDto>>>
{
    public Guid UserId { get; set; }
    public NotificationType? Type { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetNotificationTemplateQuery : IRequest<ApiResponse<NotificationTemplateDto>>
{
    public Guid Id { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Recipient { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}

public class NotificationTemplateDto
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

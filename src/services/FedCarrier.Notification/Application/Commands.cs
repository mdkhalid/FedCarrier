using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Notification.Domain;

namespace FedCarrier.Notification.Application.Commands;

public class SendNotificationCommand : IRequest<ApiResponse<Guid>>
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Recipient { get; set; }
}

public class MarkNotificationSentCommand : IRequest<ApiResponse<Unit>>
{
    public Guid NotificationId { get; set; }
}

public class CreateNotificationTemplateCommand : IRequest<ApiResponse<Guid>>
{
    public NotificationType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}

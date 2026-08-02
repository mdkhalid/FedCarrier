using FedCarrier.Contracts;
using FedCarrier.Notification.Application.Commands;
using FedCarrier.Notification.Application.Queries;
using FedCarrier.Notification.Domain;
using FedCarrier.Notification.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Notification.Application.Handlers;

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, ApiResponse<Guid>>
{
    private readonly NotificationDbContext _db;
    public SendNotificationCommandHandler(NotificationDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(SendNotificationCommand request, CancellationToken ct)
    {
        var notification = new Domain.Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Type = request.Type,
            Channel = request.Channel,
            Status = NotificationStatus.Pending,
            Subject = request.Subject,
            Body = request.Body,
            Recipient = request.Recipient,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = notification.Id };
    }
}

public class MarkNotificationSentCommandHandler : IRequestHandler<MarkNotificationSentCommand, ApiResponse<Unit>>
{
    private readonly NotificationDbContext _db;
    public MarkNotificationSentCommandHandler(NotificationDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(MarkNotificationSentCommand request, CancellationToken ct)
    {
        var notification = await _db.Notifications.FindAsync([request.NotificationId], ct);
        if (notification is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Notification not found" } };

        notification.Status = NotificationStatus.Sent;
        notification.SentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class CreateNotificationTemplateCommandHandler : IRequestHandler<CreateNotificationTemplateCommand, ApiResponse<Guid>>
{
    private readonly NotificationDbContext _db;
    public CreateNotificationTemplateCommandHandler(NotificationDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(CreateNotificationTemplateCommand request, CancellationToken ct)
    {
        var template = new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Name = request.Name,
            SubjectTemplate = request.SubjectTemplate,
            BodyTemplate = request.BodyTemplate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.NotificationTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = template.Id };
    }
}

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, ApiResponse<PagedResult<NotificationDto>>>
{
    private readonly NotificationDbContext _db;
    public GetNotificationsQueryHandler(NotificationDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<NotificationDto>>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var query = _db.Notifications
            .Where(n => n.UserId == request.UserId);

        if (request.Type.HasValue)
            query = query.Where(n => n.Type == request.Type.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                Channel = n.Channel,
                Status = n.Status,
                Subject = n.Subject,
                Body = n.Body,
                Recipient = n.Recipient,
                CreatedAt = n.CreatedAt,
                SentAt = n.SentAt
            })
            .ToListAsync(ct);

        return new ApiResponse<PagedResult<NotificationDto>>
        {
            Success = true,
            Data = new PagedResult<NotificationDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}

public class GetNotificationTemplateQueryHandler : IRequestHandler<GetNotificationTemplateQuery, ApiResponse<NotificationTemplateDto>>
{
    private readonly NotificationDbContext _db;
    public GetNotificationTemplateQueryHandler(NotificationDbContext db) => _db = db;

    public async Task<ApiResponse<NotificationTemplateDto>> Handle(GetNotificationTemplateQuery request, CancellationToken ct)
    {
        var template = await _db.NotificationTemplates.FindAsync([request.Id], ct);
        if (template is null)
            return new ApiResponse<NotificationTemplateDto> { Success = false, Errors = new List<string> { "Template not found" } };

        return new ApiResponse<NotificationTemplateDto>
        {
            Success = true,
            Data = new NotificationTemplateDto
            {
                Id = template.Id,
                Type = template.Type,
                Name = template.Name,
                SubjectTemplate = template.SubjectTemplate,
                BodyTemplate = template.BodyTemplate,
                IsActive = template.IsActive
            }
        };
    }
}

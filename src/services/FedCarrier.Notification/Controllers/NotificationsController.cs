using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FedCarrier.Contracts;
using FedCarrier.Notification.Application.Commands;
using FedCarrier.Notification.Application.Queries;
using FedCarrier.Notification.Domain;
using MediatR;

namespace FedCarrier.Notification.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ISender _mediator;

    public NotificationsController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ApiResponse<Guid>> Send([FromBody] SendNotificationCommand command)
    {
        return await _mediator.Send(command);
    }

    [HttpGet("user/{userId}")]
    public async Task<ApiResponse<PagedResult<NotificationDto>>> GetForUser(Guid userId, [FromQuery] GetNotificationsQuery query)
    {
        query.UserId = userId;
        return await _mediator.Send(query);
    }

    [HttpPost("templates")]
    public async Task<ApiResponse<Guid>> CreateTemplate([FromBody] CreateNotificationTemplateCommand command)
    {
        return await _mediator.Send(command);
    }

    [HttpGet("templates/{id}")]
    public async Task<ApiResponse<NotificationTemplateDto>> GetTemplate(Guid id)
    {
        return await _mediator.Send(new GetNotificationTemplateQuery { Id = id });
    }

    [HttpPost("{id}/sent")]
    public async Task<ApiResponse<Unit>> MarkSent(Guid id)
    {
        return await _mediator.Send(new MarkNotificationSentCommand { NotificationId = id });
    }
}

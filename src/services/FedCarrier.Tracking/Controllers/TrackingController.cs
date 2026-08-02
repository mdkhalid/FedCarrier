using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FedCarrier.Contracts;
using FedCarrier.Tracking.Application.Commands;
using FedCarrier.Tracking.Application.Queries;
using FedCarrier.Tracking.Domain;
using MediatR;

namespace FedCarrier.Tracking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IHubContext<TrackingHub> _hubContext;

    public TrackingController(ISender mediator, IHubContext<TrackingHub> hubContext)
    {
        _mediator = mediator;
        _hubContext = hubContext;
    }

    [HttpPost("location")]
    public async Task<ApiResponse<Guid>> PostLocation([FromBody] CreateTrackingLocationCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.Success && result.Data != Guid.Empty)
        {
            await _hubContext.Clients.Group("Shipment_" + command.ShipmentId)
                .SendAsync("LocationUpdated", new
                {
                    shipmentId = command.ShipmentId,
                    latitude = command.Latitude,
                    longitude = command.Longitude,
                    timestamp = DateTime.UtcNow,
                    status = command.Status.ToString()
                });
        }
        return result;
    }

    [HttpGet("{shipmentId}")]
    public async Task<ApiResponse<TrackingLocationDto>> GetCurrent(Guid shipmentId)
    {
        return await _mediator.Send(new GetCurrentTrackingQuery { ShipmentId = shipmentId });
    }

    [HttpGet("history/{shipmentId}")]
    public async Task<ApiResponse<List<TrackingLocationDto>>> GetHistory(Guid shipmentId)
    {
        return await _mediator.Send(new GetTrackingHistoryQuery { ShipmentId = shipmentId });
    }

    [HttpPut("status")]
    public async Task<ApiResponse<Unit>> UpdateStatus([FromBody] UpdateTrackingStatusCommand command)
    {
        return await _mediator.Send(command);
    }
}

public class TrackingHub : Hub
{
    public async Task JoinShipment(Guid shipmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Shipment_" + shipmentId);
    }

    public async Task LeaveShipment(Guid shipmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Shipment_" + shipmentId);
    }
}



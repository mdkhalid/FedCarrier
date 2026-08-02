using FedCarrier.Common;
using FedCarrier.Contracts;
using FedCarrier.Shipment.Application.Commands;
using FedCarrier.Shipment.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FedCarrier.Shipment;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly ISender _mediator;

    public ShipmentsController(ISender mediator) => _mediator = mediator;

    private string CorrelationId =>
        Request.Headers.TryGetValue(Constants.CorrelationIdHeader, out var value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(CreateShipmentCommand command)
    {
        command.CorrelationId = CorrelationId;
        return Ok(await _mediator.Send(command));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Get(Guid id)
        => Ok(await _mediator.Send(new GetShipmentQuery { Id = id }));

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ShipmentSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ShipmentSummaryDto>>>> Search([FromQuery] SearchShipmentsQuery query)
        => Ok(await _mediator.Send(query));

    [HttpPut("{id}/assign-driver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> AssignDriver(Guid id, AssignDriverCommand command)
    {
        command.ShipmentId = id;
        command.CorrelationId = CorrelationId;
        return Ok(await _mediator.Send(command));
    }

    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateStatus(Guid id, UpdateShipmentStatusCommand command)
    {
        command.ShipmentId = id;
        command.CorrelationId = CorrelationId;
        return Ok(await _mediator.Send(command));
    }
}

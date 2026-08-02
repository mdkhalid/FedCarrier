using FedCarrier.Contracts;
using FedCarrier.Fleet.Application.Commands;
using FedCarrier.Fleet.Application.Queries;
using FedCarrier.Fleet.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FedCarrier.Fleet;

[ApiController]
[Route("api/fleet")]
[Authorize]
public class FleetController : ControllerBase
{
    private readonly ISender _mediator;

    public FleetController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(CreateVehicleCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VehicleDto>>> Get(Guid id)
        => Ok(await _mediator.Send(new GetVehicleQuery { Id = id }));

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VehicleSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleSummaryDto>>>> GetAll([FromQuery] GetAllVehiclesQuery query)
        => Ok(await _mediator.Send(query));

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(Guid id, UpdateVehicleCommand command)
    {
        command.Id = id;
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id)
        => Ok(await _mediator.Send(new DeleteVehicleCommand { Id = id }));

    [HttpPut("{id}/assign-driver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> AssignDriver(Guid id, AssignDriverCommand command)
    {
        command.VehicleId = id;
        return Ok(await _mediator.Send(command));
    }
}

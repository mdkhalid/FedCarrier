using FedCarrier.Contracts;
using FedCarrier.Driver.Application.Commands;
using FedCarrier.Driver.Application.Queries;
using FedCarrier.Driver.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FedCarrier.Driver;

[ApiController]
[Route("api/drivers")]
[Authorize]
public class DriversController : ControllerBase
{
    private readonly ISender _mediator;

    public DriversController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(CreateDriverCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DriverDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DriverDto>>> Get(Guid id)
        => Ok(await _mediator.Send(new GetDriverQuery { Id = id }));

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DriverSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DriverSummaryDto>>>> GetAll([FromQuery] GetAllDriversQuery query)
        => Ok(await _mediator.Send(query));

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(Guid id, UpdateDriverCommand command)
    {
        command.Id = id;
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id)
        => Ok(await _mediator.Send(new DeleteDriverCommand { Id = id }));

    [HttpPut("{id}/location")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateLocation(Guid id, UpdateDriverLocationCommand command)
    {
        command.DriverId = id;
        return Ok(await _mediator.Send(command));
    }

    [HttpPut("{id}/availability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateAvailability(Guid id, UpdateDriverAvailabilityCommand command)
    {
        command.DriverId = id;
        return Ok(await _mediator.Send(command));
    }
}

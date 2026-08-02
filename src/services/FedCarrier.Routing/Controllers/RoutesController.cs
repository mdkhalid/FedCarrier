using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FedCarrier.Contracts;
using FedCarrier.Routing.Application.Commands;
using FedCarrier.Routing.Application.Queries;
using FedCarrier.Routing.Domain;
using MediatR;

namespace FedCarrier.Routing.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoutesController : ControllerBase
{
    private readonly ISender _mediator;

    public RoutesController(ISender mediator) => _mediator = mediator;

    [HttpPost("optimize")]
    public async Task<ApiResponse<Guid>> Optimize([FromBody] OptimizeRouteCommand command)
    {
        return await _mediator.Send(command);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<RoutePlanDto>> GetById(Guid id)
    {
        return await _mediator.Send(new GetRoutePlanQuery { Id = id });
    }

    [HttpGet("active/{driverId}")]
    public async Task<ApiResponse<List<RoutePlanDto>>> GetActive(Guid driverId)
    {
        return await _mediator.Send(new GetActiveRoutesQuery { DriverId = driverId });
    }

    [HttpPut("status")]
    public async Task<ApiResponse<Unit>> UpdateStatus([FromBody] UpdateRouteStatusCommand command)
    {
        return await _mediator.Send(command);
    }
}

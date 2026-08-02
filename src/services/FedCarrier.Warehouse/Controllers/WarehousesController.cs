using FedCarrier.Contracts;
using FedCarrier.Warehouse.Application.Commands;
using FedCarrier.Warehouse.Application.Queries;
using FedCarrier.Warehouse.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FedCarrier.Warehouse;

[ApiController]
[Route("api/warehouses")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly ISender _mediator;

    public WarehousesController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(CreateWarehouseCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> Get(Guid id)
        => Ok(await _mediator.Send(new GetWarehouseQuery { Id = id }));

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<WarehouseSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<WarehouseSummaryDto>>>> GetAll([FromQuery] GetAllWarehousesQuery query)
        => Ok(await _mediator.Send(query));

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(Guid id, UpdateWarehouseCommand command)
    {
        command.Id = id;
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id)
        => Ok(await _mediator.Send(new DeleteWarehouseCommand { Id = id }));

    [HttpPost("{id}/inventory")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> AddInventory(Guid id, AddInventoryCommand command)
    {
        command.WarehouseId = id;
        return Ok(await _mediator.Send(command));
    }
}

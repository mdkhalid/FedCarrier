using FedCarrier.Common;
using FedCarrier.Contracts;
using FedCarrier.Order.Application.Commands;
using FedCarrier.Order.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FedCarrier.Order;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ISender _mediator;

    public OrdersController(ISender mediator) => _mediator = mediator;

    private string CorrelationId =>
        Request.Headers.TryGetValue(Constants.CorrelationIdHeader, out var value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(CreateOrderCommand command)
    {
        command.CorrelationId = CorrelationId;
        return Ok(await _mediator.Send(command));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Get(Guid id)
        => Ok(await _mediator.Send(new GetOrderQuery { Id = id }));

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderSummaryDto>>>> GetAll([FromQuery] GetAllOrdersQuery query)
        => Ok(await _mediator.Send(query));

    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateStatus(Guid id, UpdateOrderStatusCommand command)
    {
        command.Id = id;
        command.CorrelationId = CorrelationId;
        return Ok(await _mediator.Send(command));
    }

    [HttpPut("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> Cancel(Guid id)
        => Ok(await _mediator.Send(new CancelOrderCommand { Id = id, CorrelationId = CorrelationId }));
}

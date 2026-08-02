using FedCarrier.Contracts;
using FedCarrier.Customer.Application.Commands;
using FedCarrier.Customer.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FedCarrier.Customer;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ISender _mediator;

    public CustomersController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(CreateCustomerCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Get(Guid id)
        => Ok(await _mediator.Send(new GetCustomerQuery { Id = id }));

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomerSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerSummaryDto>>>> GetAll([FromQuery] GetAllCustomersQuery query)
        => Ok(await _mediator.Send(query));

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(Guid id, UpdateCustomerCommand command)
    {
        command.Id = id;
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id)
        => Ok(await _mediator.Send(new DeleteCustomerCommand { Id = id }));

    [HttpPost("{id}/addresses")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> AddAddress(Guid id, AddAddressCommand command)
    {
        command.CustomerId = id;
        return Ok(await _mediator.Send(command));
    }

    [HttpGet("{id}/addresses")]
    [ProducesResponseType(typeof(ApiResponse<List<AddressDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AddressDto>>>> GetAddresses(Guid id)
        => Ok(await _mediator.Send(new GetCustomerAddressesQuery { CustomerId = id }));
}

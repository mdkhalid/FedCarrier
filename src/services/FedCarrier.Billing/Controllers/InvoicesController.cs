using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FedCarrier.Common;
using FedCarrier.Contracts;
using FedCarrier.Billing.Application.Commands;
using FedCarrier.Billing.Application.Queries;
using FedCarrier.Billing.Domain;
using MediatR;

namespace FedCarrier.Billing.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly ISender _mediator;

    public InvoicesController(ISender mediator) => _mediator = mediator;

    private string CorrelationId =>
        Request.Headers.TryGetValue(Constants.CorrelationIdHeader, out var value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

    [HttpPost]
    public async Task<ApiResponse<Guid>> Create([FromBody] CreateInvoiceCommand command)
    {
        command.CorrelationId = CorrelationId;
        return await _mediator.Send(command);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<InvoiceDto>> GetById(Guid id)
    {
        return await _mediator.Send(new GetInvoiceQuery { Id = id });
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<InvoiceSummaryDto>>> Search([FromQuery] SearchInvoicesQuery query)
    {
        return await _mediator.Send(query);
    }

    [HttpPost("{id}/pay")]
    public async Task<ApiResponse<Unit>> ConfirmPayment(Guid id)
    {
        return await _mediator.Send(new ConfirmPaymentCommand { InvoiceId = id, CorrelationId = CorrelationId });
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse<Unit>> Cancel(Guid id)
    {
        return await _mediator.Send(new CancelInvoiceCommand { InvoiceId = id, CorrelationId = CorrelationId });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FedCarrier.Contracts;
using FedCarrier.Reporting.Application.Commands;
using FedCarrier.Reporting.Application.Queries;
using FedCarrier.Reporting.Domain;
using MediatR;

namespace FedCarrier.Reporting.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ISender _mediator;

    public ReportsController(ISender mediator) => _mediator = mediator;

    [HttpPost("generate")]
    public async Task<ApiResponse<Guid>> Generate([FromBody] GenerateReportCommand command)
    {
        return await _mediator.Send(command);
    }

    [HttpGet("{id}")]
    public async Task<ApiResponse<ReportDto>> GetById(Guid id)
    {
        return await _mediator.Send(new GetReportQuery { Id = id });
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<ReportSummaryDto>>> GetAll([FromQuery] GetReportsQuery query)
    {
        return await _mediator.Send(query);
    }

    [HttpPost("definitions")]
    public async Task<ApiResponse<Guid>> CreateDefinition([FromBody] CreateReportDefinitionCommand command)
    {
        return await _mediator.Send(command);
    }
}

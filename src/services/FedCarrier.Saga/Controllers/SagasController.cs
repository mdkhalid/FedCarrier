using FedCarrier.Contracts;
using FedCarrier.Saga.Domain;
using FedCarrier.Saga.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Saga.Controllers;

public class SagaStateDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ShipmentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid CustomerId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

[ApiController]
[Route("api/sagas")]
[Authorize]
public class SagasController : ControllerBase
{
    private readonly SagaDbContext _db;

    public SagasController(SagaDbContext db) => _db = db;

    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(ApiResponse<SagaStateDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<SagaStateDto>> Get(Guid orderId)
    {
        var saga = await _db.SagaStates.FirstOrDefaultAsync(s => s.OrderId == orderId);
        if (saga is null)
            return new ApiResponse<SagaStateDto> { Success = false, Errors = new List<string> { "Saga not found" } };

        return new ApiResponse<SagaStateDto>
        {
            Success = true,
            Data = new SagaStateDto
            {
                Id = saga.Id,
                OrderId = saga.OrderId,
                ShipmentId = saga.ShipmentId,
                InvoiceId = saga.InvoiceId,
                CustomerId = saga.CustomerId,
                CorrelationId = saga.CorrelationId,
                CurrentStep = saga.CurrentStep.ToString(),
                Status = saga.Status.ToString(),
                FailureReason = saga.FailureReason,
                CreatedAt = saga.CreatedAt,
                CompletedAt = saga.CompletedAt
            }
        };
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SagaStateDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PagedResult<SagaStateDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var totalCount = await _db.SagaStates.CountAsync();
        var items = await _db.SagaStates
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SagaStateDto
            {
                Id = s.Id,
                OrderId = s.OrderId,
                ShipmentId = s.ShipmentId,
                InvoiceId = s.InvoiceId,
                CustomerId = s.CustomerId,
                CorrelationId = s.CorrelationId,
                CurrentStep = s.CurrentStep.ToString(),
                Status = s.Status.ToString(),
                FailureReason = s.FailureReason,
                CreatedAt = s.CreatedAt,
                CompletedAt = s.CompletedAt
            })
            .ToListAsync();

        return new ApiResponse<PagedResult<SagaStateDto>>
        {
            Success = true,
            Data = new PagedResult<SagaStateDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize }
        };
    }
}

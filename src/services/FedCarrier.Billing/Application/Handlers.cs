using FedCarrier.Contracts;
using FedCarrier.Billing.Application.Commands;
using FedCarrier.Billing.Application.Queries;
using FedCarrier.Billing.Domain;
using FedCarrier.Billing.Infrastructure;
using FedCarrier.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Billing.Application.Handlers;

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, ApiResponse<Guid>>
{
    private readonly BillingDbContext _db;
    private readonly IOutboxRepository? _outbox;
    public CreateInvoiceCommandHandler(BillingDbContext db, IOutboxRepository? outbox = null)
    {
        _db = db;
        _outbox = outbox;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        var taxAmount = request.Amount * request.TaxRate;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ShipmentId = request.ShipmentId,
            CustomerId = request.CustomerId,
            InvoiceNumber = GenerateInvoiceNumber(),
            Amount = request.Amount,
            TaxAmount = taxAmount,
            TotalAmount = request.Amount + taxAmount,
            Status = InvoiceStatus.Draft,
            IssueDate = DateTime.UtcNow,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice
            });
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        await OutboxWriter.WriteAsync(_outbox, new InvoiceGeneratedEvent
        {
            InvoiceId = invoice.Id,
            ShipmentId = invoice.ShipmentId,
            CustomerId = invoice.CustomerId,
            InvoiceNumber = invoice.InvoiceNumber,
            TotalAmount = invoice.TotalAmount,
            CorrelationId = request.CorrelationId
        }, invoice.Id.ToString(), ct);

        return new ApiResponse<Guid> { Success = true, Data = invoice.Id };
    }

    private static string GenerateInvoiceNumber()
    {
        return "INV-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }
}

public class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, ApiResponse<Unit>>
{
    private readonly BillingDbContext _db;
    private readonly IOutboxRepository? _outbox;
    public ConfirmPaymentCommandHandler(BillingDbContext db, IOutboxRepository? outbox = null)
    {
        _db = db;
        _outbox = outbox;
    }

    public async Task<ApiResponse<Unit>> Handle(ConfirmPaymentCommand request, CancellationToken ct)
    {
        var invoice = await _db.Invoices.FindAsync([request.InvoiceId], ct);
        if (invoice is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Invoice not found" } };

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await OutboxWriter.WriteAsync(_outbox, new PaymentConfirmedEvent
        {
            InvoiceId = invoice.Id,
            ShipmentId = invoice.ShipmentId,
            CustomerId = invoice.CustomerId,
            TotalAmount = invoice.TotalAmount,
            CorrelationId = request.CorrelationId
        }, invoice.Id.ToString(), ct);

        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class CancelInvoiceCommandHandler : IRequestHandler<CancelInvoiceCommand, ApiResponse<Unit>>
{
    private readonly BillingDbContext _db;
    public CancelInvoiceCommandHandler(BillingDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(CancelInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _db.Invoices.FindAsync([request.InvoiceId], ct);
        if (invoice is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Invoice not found" } };

        invoice.Status = InvoiceStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class GetInvoiceQueryHandler : IRequestHandler<GetInvoiceQuery, ApiResponse<InvoiceDto>>
{
    private readonly BillingDbContext _db;
    public GetInvoiceQueryHandler(BillingDbContext db) => _db = db;

    public async Task<ApiResponse<InvoiceDto>> Handle(GetInvoiceQuery request, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (invoice is null)
            return new ApiResponse<InvoiceDto> { Success = false, Errors = new List<string> { "Invoice not found" } };

        return new ApiResponse<InvoiceDto>
        {
            Success = true,
            Data = new InvoiceDto
            {
                Id = invoice.Id,
                ShipmentId = invoice.ShipmentId,
                CustomerId = invoice.CustomerId,
                InvoiceNumber = invoice.InvoiceNumber,
                Amount = invoice.Amount,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                PaidAt = invoice.PaidAt,
                Items = invoice.Items.Select(i => new InvoiceItemDto
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            }
        };
    }
}

public class SearchInvoicesQueryHandler : IRequestHandler<SearchInvoicesQuery, ApiResponse<PagedResult<InvoiceSummaryDto>>>
{
    private readonly BillingDbContext _db;
    public SearchInvoicesQueryHandler(BillingDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<InvoiceSummaryDto>>> Handle(SearchInvoicesQuery request, CancellationToken ct)
    {
        var query = _db.Invoices.AsQueryable();

        if (request.CustomerId.HasValue)
            query = query.Where(i => i.CustomerId == request.CustomerId.Value);
        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);
        if (request.FromDate.HasValue)
            query = query.Where(i => i.IssueDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(i => i.IssueDate <= request.ToDate.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceSummaryDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                ShipmentId = i.ShipmentId,
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                IssueDate = i.IssueDate
            })
            .ToListAsync(ct);

        return new ApiResponse<PagedResult<InvoiceSummaryDto>>
        {
            Success = true,
            Data = new PagedResult<InvoiceSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}

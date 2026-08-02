using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Billing.Domain;

namespace FedCarrier.Billing.Application.Commands;

public class CreateInvoiceItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CreateInvoiceCommand : IRequest<ApiResponse<Guid>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid ShipmentId { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public DateTime DueDate { get; set; }
    public List<CreateInvoiceItemDto> Items { get; set; } = new();
}

public class ConfirmPaymentCommand : IRequest<ApiResponse<Unit>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
}

public class CancelInvoiceCommand : IRequest<ApiResponse<Unit>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
}

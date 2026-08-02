using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Order.Domain;

namespace FedCarrier.Order.Application.Queries;

public class GetOrderQuery : IRequest<ApiResponse<OrderDto>>
{
    public Guid Id { get; set; }
}

public class GetAllOrdersQuery : IRequest<ApiResponse<PagedResult<OrderSummaryDto>>>
{
    public Guid? CustomerId { get; set; }
    public OrderStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}

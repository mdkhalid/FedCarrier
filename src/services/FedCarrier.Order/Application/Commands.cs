using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Order.Domain;

namespace FedCarrier.Order.Application.Commands;

public class CreateOrderCommand : IRequest<ApiResponse<Guid>>
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class UpdateOrderStatusCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
    public OrderStatus Status { get; set; }
}

public class CancelOrderCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
}

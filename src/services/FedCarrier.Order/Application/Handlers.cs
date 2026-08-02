using FedCarrier.Contracts;
using FedCarrier.Order.Application.Commands;
using FedCarrier.Order.Application.Queries;
using FedCarrier.Order.Domain;
using FedCarrier.Order.Infrastructure;
using FedCarrier.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Order.Application.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ApiResponse<Guid>>
{
    private readonly OrderDbContext _db;
    public CreateOrderCommandHandler(OrderDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = new FedCarrier.Order.Domain.Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice)
        };

        foreach (var item in request.Items)
        {
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = order.Id };
    }
}

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, ApiResponse<Unit>>
{
    private readonly OrderDbContext _db;
    public UpdateOrderStatusCommandHandler(OrderDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync(new object[] { request.Id }, ct);
        if (order is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Order not found" } };

        order.Status = request.Status;
        if (request.Status == OrderStatus.Shipped)
            order.ShippedDate = DateTime.UtcNow;
        if (request.Status == OrderStatus.Delivered)
            order.DeliveredDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, ApiResponse<Unit>>
{
    private readonly OrderDbContext _db;
    public CancelOrderCommandHandler(OrderDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync(new object[] { request.Id }, ct);
        if (order is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Order not found" } };

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, ApiResponse<OrderDto>>
{
    private readonly OrderDbContext _db;
    public GetOrderQueryHandler(OrderDbContext db) => _db = db;

    public async Task<ApiResponse<OrderDto>> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct);

        if (order is null)
            return new ApiResponse<OrderDto> { Success = false, Errors = new List<string> { "Order not found" } };

        return new ApiResponse<OrderDto>
        {
            Success = true,
            Data = new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderDate = order.OrderDate,
                ShippedDate = order.ShippedDate,
                DeliveredDate = order.DeliveredDate,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            }
        };
    }
}

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, ApiResponse<PagedResult<OrderSummaryDto>>>
{
    private readonly OrderDbContext _db;
    public GetAllOrdersQueryHandler(OrderDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<OrderSummaryDto>>> Handle(GetAllOrdersQuery request, CancellationToken ct)
    {
        var query = _db.Orders.AsQueryable();

        if (request.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);
        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);
        if (request.FromDate.HasValue)
            query = query.Where(o => o.OrderDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(o => o.OrderDate <= request.ToDate.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                CustomerName = o.CustomerName,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                OrderDate = o.OrderDate
            })
            .ToListAsync(ct);

        return new ApiResponse<PagedResult<OrderSummaryDto>>
        {
            Success = true,
            Data = new PagedResult<OrderSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}

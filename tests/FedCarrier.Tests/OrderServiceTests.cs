using FedCarrier.Contracts;
using FedCarrier.Order.Application.Commands;
using FedCarrier.Order.Application.Handlers;
using FedCarrier.Order.Application.Queries;
using FedCarrier.Order.Domain;
using FedCarrier.Order.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class OrderServiceTests
{
    private OrderDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    [Fact]
    public async Task CreateOrderCommandHandler_ShouldCreateOrderWithTotal()
    {
        var db = GetDbContext();
        var handler = new CreateOrderCommandHandler(db);
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Alice Smith",
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductName = "Widget", Quantity = 2, UnitPrice = 25m },
                new CreateOrderItemDto { ProductName = "Gadget", Quantity = 1, UnitPrice = 50m }
            }
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var order = await db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == result.Data);
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(100m);
        order.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateOrderStatusCommandHandler_ShouldMarkOrderShipped()
    {
        var db = GetDbContext();
        var createHandler = new CreateOrderCommandHandler(db);
        var orderId = (await createHandler.Handle(new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Bob Brown",
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductName = "Widget", Quantity = 1, UnitPrice = 10m }
            }
        }, CancellationToken.None)).Data;

        var handler = new UpdateOrderStatusCommandHandler(db);
        var command = new UpdateOrderStatusCommand { Id = orderId, Status = OrderStatus.Shipped };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var order = await db.Orders.FindAsync(orderId);
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelOrderCommandHandler_ShouldCancelOrder()
    {
        var db = GetDbContext();
        var createHandler = new CreateOrderCommandHandler(db);
        var orderId = (await createHandler.Handle(new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Carol White",
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductName = "Widget", Quantity = 1, UnitPrice = 10m }
            }
        }, CancellationToken.None)).Data;

        var handler = new CancelOrderCommandHandler(db);
        var command = new CancelOrderCommand { Id = orderId };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var order = await db.Orders.FindAsync(orderId);
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task GetOrderQueryHandler_ShouldReturnOrderWithItems()
    {
        var db = GetDbContext();
        var createHandler = new CreateOrderCommandHandler(db);
        var orderId = (await createHandler.Handle(new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Dave Green",
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductName = "Widget", Quantity = 3, UnitPrice = 20m }
            }
        }, CancellationToken.None)).Data;

        var handler = new GetOrderQueryHandler(db);
        var query = new GetOrderQuery { Id = orderId };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.CustomerName.Should().Be("Dave Green");
        result.Data.TotalAmount.Should().Be(60m);
        result.Data.Items.Should().HaveCount(1);
    }
}

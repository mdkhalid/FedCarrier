using FedCarrier.Contracts;
using FedCarrier.Shipment.Application.Commands;
using FedCarrier.Shipment.Application.Handlers;
using FedCarrier.Shipment.Application.Queries;
using FedCarrier.Shipment.Domain;
using FedCarrier.Shipment.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class ShipmentServiceTests
{
    private ShipmentDbContext GetDbContext(string? name = null)
    {
        var options = new DbContextOptionsBuilder<ShipmentDbContext>()
            .UseInMemoryDatabase(databaseName: name ?? Guid.NewGuid().ToString())
            .Options;
        return new ShipmentDbContext(options);
    }

    [Fact]
    public async Task CreateShipmentCommandHandler_ShouldCreateShipmentWithItems()
    {
        var db = GetDbContext();
        var handler = new CreateShipmentCommandHandler(db);
        var command = new CreateShipmentCommand
        {
            OrderId = Guid.NewGuid(),
            Origin = "Cairo",
            Destination = "Alexandria",
            Items = new List<CreateShipmentItemDto>
            {
                new CreateShipmentItemDto { Description = "Electronics", Weight = 5m, Quantity = 2, Price = 100m }
            }
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var shipment = await db.Shipments.Include(s => s.Items).FirstAsync(s => s.Id == result.Data);
        shipment.Status.Should().Be(ShipmentStatus.Pending);
        shipment.Items.Should().HaveCount(1);
        shipment.StatusHistory.Should().HaveCount(1);
    }

    [Fact]
    public async Task AssignDriverCommandHandler_ShouldAssignDriverAndSetStatus()
    {
        var storeName = Guid.NewGuid().ToString();
        var createDb = GetDbContext(storeName);
        var createHandler = new CreateShipmentCommandHandler(createDb);
        var shipmentId = (await createHandler.Handle(new CreateShipmentCommand
        {
            OrderId = Guid.NewGuid(),
            Origin = "Cairo",
            Destination = "Giza"
        }, CancellationToken.None)).Data;

        var db = GetDbContext(storeName);
        var handler = new AssignDriverCommandHandler(db);
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var command = new AssignDriverCommand
        {
            ShipmentId = shipmentId,
            DriverId = driverId,
            VehicleId = vehicleId
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var shipment = await db.Shipments.FindAsync(shipmentId);
        shipment.AssignedDriverId.Should().Be(driverId);
        shipment.VehicleId.Should().Be(vehicleId);
        shipment.Status.Should().Be(ShipmentStatus.Assigned);
    }

    [Fact]
    public async Task GetShipmentQueryHandler_ShouldReturnShipment()
    {
        var db = GetDbContext();
        var createHandler = new CreateShipmentCommandHandler(db);
        var shipmentId = (await createHandler.Handle(new CreateShipmentCommand
        {
            OrderId = Guid.NewGuid(),
            Origin = "Cairo",
            Destination = "Luxor",
            Items = new List<CreateShipmentItemDto>
            {
                new CreateShipmentItemDto { Description = "Furniture", Weight = 20m, Quantity = 1, Price = 500m }
            }
        }, CancellationToken.None)).Data;

        var handler = new GetShipmentQueryHandler(db);
        var query = new GetShipmentQuery { Id = shipmentId };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Origin.Should().Be("Cairo");
        result.Data.Destination.Should().Be("Luxor");
        result.Data.Items.Should().HaveCount(1);
        result.Data.StatusHistory.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchShipmentsQueryHandler_ShouldFilterByStatus()
    {
        var db = GetDbContext();
        var createHandler = new CreateShipmentCommandHandler(db);
        await createHandler.Handle(new CreateShipmentCommand { OrderId = Guid.NewGuid(), Origin = "Cairo", Destination = "Aswan" }, CancellationToken.None);

        var handler = new SearchShipmentsQueryHandler(db);
        var query = new SearchShipmentsQuery { Status = ShipmentStatus.Pending, Page = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items[0].Status.Should().Be(ShipmentStatus.Pending);
    }
}

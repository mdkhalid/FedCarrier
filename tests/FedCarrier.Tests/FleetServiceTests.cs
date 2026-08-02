using FedCarrier.Contracts;
using FedCarrier.Fleet.Application.Commands;
using FedCarrier.Fleet.Application.Handlers;
using FedCarrier.Fleet.Application.Queries;
using FedCarrier.Fleet.Domain;
using FedCarrier.Fleet.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FedCarrier.Tests;

public class FleetServiceTests
{
    private FleetDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<FleetDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FleetDbContext(options);
    }

    [Fact]
    public async Task CreateVehicleCommandHandler_ShouldCreateVehicle()
    {
        var db = GetDbContext();
        var handler = new CreateVehicleCommandHandler(db);
        var command = new CreateVehicleCommand
        {
            LicensePlate = "ABC123",
            Make = "Ford",
            Model = "F-150",
            Year = 2024,
            CapacityWeight = 1000m
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetVehicleQueryHandler_ShouldReturnVehicle()
    {
        var db = GetDbContext();
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            LicensePlate = "ABC123",
            Make = "Ford",
            Model = "F-150",
            Year = 2024,
            CapacityWeight = 1000m,
            Status = VehicleStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var handler = new GetVehicleQueryHandler(db);
        var query = new GetVehicleQuery { Id = vehicle.Id };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.LicensePlate.Should().Be("ABC123");
    }

    [Fact]
    public async Task GetAllVehiclesQueryHandler_ShouldReturnPagedResult()
    {
        var db = GetDbContext();
        db.Vehicles.Add(new Vehicle
        {
            Id = Guid.NewGuid(),
            LicensePlate = "ABC123",
            Make = "Ford",
            Model = "F-150",
            Year = 2024,
            CapacityWeight = 1000m,
            Status = VehicleStatus.Available,
            CreatedAt = DateTime.UtcNow
        });
        db.Vehicles.Add(new Vehicle
        {
            Id = Guid.NewGuid(),
            LicensePlate = "XYZ789",
            Make = "Chevrolet",
            Model = "Silverado",
            Year = 2023,
            CapacityWeight = 1200m,
            Status = VehicleStatus.Maintenance,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetAllVehiclesQueryHandler(db);
        var query = new GetAllVehiclesQuery { Page = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
    }
}



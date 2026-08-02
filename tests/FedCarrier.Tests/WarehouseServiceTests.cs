using FedCarrier.Contracts;
using FedCarrier.Warehouse.Application.Commands;
using FedCarrier.Warehouse.Application.Handlers;
using FedCarrier.Warehouse.Application.Queries;
using FedCarrier.Warehouse.Domain;
using FedCarrier.Warehouse.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class WarehouseServiceTests
{
    private WarehouseDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WarehouseDbContext(options);
    }

    [Fact]
    public async Task CreateWarehouseCommandHandler_ShouldCreateWarehouse()
    {
        var db = GetDbContext();
        var handler = new CreateWarehouseCommandHandler(db);
        var command = new CreateWarehouseCommand
        {
            Name = "Main Warehouse",
            Address = "123 Main St",
            City = "Dallas",
            State = "TX",
            ZipCode = "75201",
            Country = "US"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWarehouseQueryHandler_ShouldReturnWarehouse()
    {
        var db = GetDbContext();
        var warehouse = new FedCarrier.Warehouse.Domain.Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Main Warehouse",
            Address = "123 Main St",
            City = "Dallas",
            State = "TX",
            ZipCode = "75201",
            Country = "US",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var handler = new GetWarehouseQueryHandler(db);
        var query = new GetWarehouseQuery { Id = warehouse.Id };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Name.Should().Be("Main Warehouse");
    }

    [Fact]
    public async Task GetAllWarehousesQueryHandler_ShouldFilterByCity()
    {
        var db = GetDbContext();
        db.Warehouses.Add(new FedCarrier.Warehouse.Domain.Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Dallas Warehouse",
            Address = "123 Main St",
            City = "Dallas",
            State = "TX",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.Warehouses.Add(new FedCarrier.Warehouse.Domain.Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Houston Warehouse",
            Address = "456 Oak Ave",
            City = "Houston",
            State = "TX",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetAllWarehousesQueryHandler(db);
        var query = new GetAllWarehousesQuery { City = "Dallas", Page = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items.First().City.Should().Be("Dallas");
    }
}


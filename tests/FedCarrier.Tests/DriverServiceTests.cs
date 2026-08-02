using FedCarrier.Contracts;
using FedCarrier.Driver.Application.Commands;
using FedCarrier.Driver.Application.Handlers;
using FedCarrier.Driver.Application.Queries;
using FedCarrier.Driver.Domain;
using FedCarrier.Driver.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class DriverServiceTests
{
    private DriverDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<DriverDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new DriverDbContext(options);
    }

    [Fact]
    public async Task CreateDriverCommandHandler_ShouldCreateDriver()
    {
        var db = GetDbContext();
        var handler = new CreateDriverCommandHandler(db);
        var command = new CreateDriverCommand
        {
            FirstName = "John",
            LastName = "Doe",
            LicenseNumber = "DL123456",
            Phone = "555-0100"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDriverQueryHandler_ShouldReturnDriver()
    {
        var db = GetDbContext();
        var driver = new FedCarrier.Driver.Domain.Driver
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            LicenseNumber = "DL123456",
            Phone = "555-0100",
            Status = DriverStatus.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Drivers.Add(driver);
        await db.SaveChangesAsync();

        var handler = new GetDriverQueryHandler(db);
        var query = new GetDriverQuery { Id = driver.Id };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetAllDriversQueryHandler_ShouldFilterByStatus()
    {
        var db = GetDbContext();
        db.Drivers.Add(new FedCarrier.Driver.Domain.Driver
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            LicenseNumber = "DL123456",
            Status = DriverStatus.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.Drivers.Add(new FedCarrier.Driver.Domain.Driver
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Smith",
            LicenseNumber = "DL789012",
            Status = DriverStatus.OnDuty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetAllDriversQueryHandler(db);
        var query = new GetAllDriversQuery { Status = DriverStatus.Available, Page = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items.First().Status.Should().Be(DriverStatus.Available);
    }
}


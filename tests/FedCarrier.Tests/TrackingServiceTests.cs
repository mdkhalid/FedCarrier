using FedCarrier.Contracts;
using FedCarrier.Tracking.Application.Commands;
using FedCarrier.Tracking.Application.Handlers;
using FedCarrier.Tracking.Application.Queries;
using FedCarrier.Tracking.Domain;
using FedCarrier.Tracking.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class TrackingServiceTests
{
    private TrackingDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<TrackingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TrackingDbContext(options);
    }

    [Fact]
    public async Task CreateTrackingLocationCommandHandler_ShouldCreateLocation()
    {
        var db = GetDbContext();
        var handler = new CreateTrackingLocationCommandHandler(db);
        var command = new CreateTrackingLocationCommand
        {
            ShipmentId = Guid.NewGuid(),
            Latitude = 30.05,
            Longitude = 31.22,
            Address = "Cairo, Egypt",
            Speed = "55",
            Heading = "NE",
            Status = TrackingStatus.InTransit
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCurrentTrackingQueryHandler_ShouldReturnLatestLocation()
    {
        var db = GetDbContext();
        var shipmentId = Guid.NewGuid();
        db.TrackingLocations.Add(new TrackingLocation
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Latitude = 30.05,
            Longitude = 31.22,
            Timestamp = DateTime.UtcNow.AddMinutes(-10),
            Status = TrackingStatus.InTransit,
            CreatedAt = DateTime.UtcNow
        });
        db.TrackingLocations.Add(new TrackingLocation
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Latitude = 30.06,
            Longitude = 31.23,
            Timestamp = DateTime.UtcNow,
            Status = TrackingStatus.InTransit,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetCurrentTrackingQueryHandler(db);
        var query = new GetCurrentTrackingQuery { ShipmentId = shipmentId };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Latitude.Should().Be(30.06);
    }

    [Fact]
    public async Task GetTrackingHistoryQueryHandler_ShouldReturnAllLocations()
    {
        var db = GetDbContext();
        var shipmentId = Guid.NewGuid();
        db.TrackingLocations.Add(new TrackingLocation
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Latitude = 30.05,
            Longitude = 31.22,
            Timestamp = DateTime.UtcNow.AddMinutes(-10),
            Status = TrackingStatus.InTransit,
            CreatedAt = DateTime.UtcNow
        });
        db.TrackingLocations.Add(new TrackingLocation
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Latitude = 30.06,
            Longitude = 31.23,
            Timestamp = DateTime.UtcNow,
            Status = TrackingStatus.InTransit,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetTrackingHistoryQueryHandler(db);
        var query = new GetTrackingHistoryQuery { ShipmentId = shipmentId };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }
}

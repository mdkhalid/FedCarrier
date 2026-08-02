using FedCarrier.Contracts;
using FedCarrier.Routing.Application.Commands;
using FedCarrier.Routing.Application.Handlers;
using FedCarrier.Routing.Application.Queries;
using FedCarrier.Routing.Domain;
using FedCarrier.Routing.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class RoutePlanningServiceTests
{
    private RouteDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<RouteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RouteDbContext(options);
    }

    [Fact]
    public async Task OptimizeRouteCommandHandler_ShouldCreateRouteWithStops()
    {
        var db = GetDbContext();
        var handler = new OptimizeRouteCommandHandler(db);
        var command = new OptimizeRouteCommand
        {
            DriverId = Guid.NewGuid(),
            ShipmentId = Guid.NewGuid(),
            OriginLatitude = 30.04,
            OriginLongitude = 31.24,
            OriginAddress = "Origin St",
            DestinationLatitude = 30.10,
            DestinationLongitude = 31.34,
            DestinationAddress = "Dest St",
            Waypoints = new List<RouteStopInput>
            {
                new RouteStopInput { Latitude = 30.06, Longitude = 31.28, Address = "Midpoint" }
            },
            TrafficLevel = TrafficLevel.Moderate
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var route = await db.RoutePlans.FindAsync(result.Data);
        route.Should().NotBeNull();
        route.TotalDistanceKm.Should().BeGreaterThan(0);
        route.EstimatedDurationMinutes.Should().BeGreaterThan(0);
        route.EstimatedArrival.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRoutePlanQueryHandler_ShouldReturnRouteWithSequencedStops()
    {
        var db = GetDbContext();
        var route = new RoutePlan
        {
            Id = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            OriginAddress = "Origin St",
            DestinationAddress = "Dest St",
            TrafficLevel = TrafficLevel.Low,
            Status = RouteStatus.Optimized,
            TotalDistanceKm = 10,
            EstimatedDurationMinutes = 20,
            EstimatedArrival = DateTime.UtcNow.AddMinutes(20),
            CreatedAt = DateTime.UtcNow
        };
        db.RoutePlans.Add(route);
        db.RouteStops.Add(new RouteStop
        {
            Id = Guid.NewGuid(),
            RoutePlanId = route.Id,
            Sequence = 0,
            Latitude = 30.04,
            Longitude = 31.24,
            Address = "Origin St"
        });
        db.RouteStops.Add(new RouteStop
        {
            Id = Guid.NewGuid(),
            RoutePlanId = route.Id,
            Sequence = 1,
            Latitude = 30.10,
            Longitude = 31.34,
            Address = "Dest St"
        });
        await db.SaveChangesAsync();

        var handler = new GetRoutePlanQueryHandler(db);
        var query = new GetRoutePlanQuery { Id = route.Id };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Stops.Should().HaveCount(2);
        result.Data.Stops[0].Sequence.Should().Be(0);
        result.Data.Stops[1].Sequence.Should().Be(1);
    }

    [Fact]
    public async Task UpdateRouteStatusCommandHandler_ShouldMarkRouteCompleted()
    {
        var db = GetDbContext();
        var route = new RoutePlan
        {
            Id = Guid.NewGuid(),
            DriverId = Guid.NewGuid(),
            TrafficLevel = TrafficLevel.Low,
            Status = RouteStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };
        db.RoutePlans.Add(route);
        db.RouteStops.Add(new RouteStop
        {
            Id = Guid.NewGuid(),
            RoutePlanId = route.Id,
            Sequence = 0,
            Latitude = 30.04,
            Longitude = 31.24
        });
        await db.SaveChangesAsync();

        var handler = new UpdateRouteStatusCommandHandler(db);
        var command = new UpdateRouteStatusCommand
        {
            RoutePlanId = route.Id,
            Status = RouteStatus.Completed
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var updated = await db.RoutePlans.FindAsync(route.Id);
        updated.Status.Should().Be(RouteStatus.Completed);
    }
}

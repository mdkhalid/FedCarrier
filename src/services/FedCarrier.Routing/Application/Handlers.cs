using FedCarrier.Contracts;
using FedCarrier.Routing.Application.Commands;
using FedCarrier.Routing.Application.Queries;
using FedCarrier.Routing.Domain;
using FedCarrier.Routing.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Routing.Application.Handlers;

public class OptimizeRouteCommandHandler : IRequestHandler<OptimizeRouteCommand, ApiResponse<Guid>>
{
    private readonly RouteDbContext _db;
    public OptimizeRouteCommandHandler(RouteDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(OptimizeRouteCommand request, CancellationToken ct)
    {
        var route = new RoutePlan
        {
            Id = Guid.NewGuid(),
            DriverId = request.DriverId,
            ShipmentId = request.ShipmentId,
            OriginAddress = request.OriginAddress,
            DestinationAddress = request.DestinationAddress,
            TrafficLevel = request.TrafficLevel,
            Status = RouteStatus.Optimized,
            CreatedAt = DateTime.UtcNow
        };

        var stops = new List<(double Lat, double Lon, string? Address)>();
        stops.Add((request.OriginLatitude, request.OriginLongitude, request.OriginAddress));
        if (request.Waypoints is not null)
            stops.AddRange(request.Waypoints.Select(w => (w.Latitude, w.Longitude, w.Address)));
        stops.Add((request.DestinationLatitude, request.DestinationLongitude, request.DestinationAddress));

        var now = DateTime.UtcNow;
        double totalKm = 0;
        for (var i = 0; i < stops.Count; i++)
        {
            if (i > 0)
                totalKm += HaversineKm(stops[i - 1].Lat, stops[i - 1].Lon, stops[i].Lat, stops[i].Lon);

            var routeStop = new RouteStop
            {
                Id = Guid.NewGuid(),
                RoutePlanId = route.Id,
                Sequence = i,
                Latitude = stops[i].Lat,
                Longitude = stops[i].Lon,
                Address = stops[i].Address,
                EstimatedArrival = now.AddMinutes(EstimateMinutes(totalKm, request.TrafficLevel))
            };

            route.Stops.Add(routeStop);
        }

        route.TotalDistanceKm = totalKm;
        route.EstimatedDurationMinutes = EstimateMinutes(totalKm, request.TrafficLevel);
        route.EstimatedArrival = now.AddMinutes(route.EstimatedDurationMinutes);

        _db.RoutePlans.Add(route);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = route.Id };
    }

    private static int EstimateMinutes(double km, TrafficLevel traffic)
    {
        var speedKmh = traffic switch
        {
            TrafficLevel.Moderate => 45,
            TrafficLevel.Heavy => 30,
            TrafficLevel.Severe => 15,
            _ => 60
        };

        return (int)Math.Ceiling(km / speedKmh * 60);
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * radius * Math.Asin(Math.Sqrt(a));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180;
}

public class UpdateRouteStatusCommandHandler : IRequestHandler<UpdateRouteStatusCommand, ApiResponse<Unit>>
{
    private readonly RouteDbContext _db;
    public UpdateRouteStatusCommandHandler(RouteDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateRouteStatusCommand request, CancellationToken ct)
    {
        var route = await _db.RoutePlans.FindAsync([request.RoutePlanId], ct);
        if (route is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Route plan not found" } };

        route.Status = request.Status;
        if (request.Status == RouteStatus.Completed)
        {
            var stops = await _db.RouteStops.Where(s => s.RoutePlanId == route.Id).ToListAsync(ct);
            foreach (var stop in stops)
            {
                stop.ActualArrival ??= DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class GetRoutePlanQueryHandler : IRequestHandler<GetRoutePlanQuery, ApiResponse<RoutePlanDto>>
{
    private readonly RouteDbContext _db;
    public GetRoutePlanQueryHandler(RouteDbContext db) => _db = db;

    public async Task<ApiResponse<RoutePlanDto>> Handle(GetRoutePlanQuery request, CancellationToken ct)
    {
        var route = await _db.RoutePlans.FindAsync([request.Id], ct);
        if (route is null)
            return new ApiResponse<RoutePlanDto> { Success = false, Errors = new List<string> { "Route plan not found" } };

        var stops = await _db.RouteStops
            .Where(s => s.RoutePlanId == route.Id)
            .OrderBy(s => s.Sequence)
            .Select(s => new RouteStopDto
            {
                Id = s.Id,
                Sequence = s.Sequence,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Address = s.Address,
                EstimatedArrival = s.EstimatedArrival,
                ActualArrival = s.ActualArrival
            })
            .ToListAsync(ct);

        return new ApiResponse<RoutePlanDto>
        {
            Success = true,
            Data = new RoutePlanDto
            {
                Id = route.Id,
                DriverId = route.DriverId,
                ShipmentId = route.ShipmentId,
                OriginAddress = route.OriginAddress,
                DestinationAddress = route.DestinationAddress,
                TrafficLevel = route.TrafficLevel,
                Status = route.Status,
                TotalDistanceKm = route.TotalDistanceKm,
                EstimatedDurationMinutes = route.EstimatedDurationMinutes,
                EstimatedArrival = route.EstimatedArrival,
                CreatedAt = route.CreatedAt,
                Stops = stops
            }
        };
    }
}

public class GetActiveRoutesQueryHandler : IRequestHandler<GetActiveRoutesQuery, ApiResponse<List<RoutePlanDto>>>
{
    private readonly RouteDbContext _db;
    public GetActiveRoutesQueryHandler(RouteDbContext db) => _db = db;

    public async Task<ApiResponse<List<RoutePlanDto>>> Handle(GetActiveRoutesQuery request, CancellationToken ct)
    {
        var routes = await _db.RoutePlans
            .Where(r => r.DriverId == request.DriverId && r.Status == RouteStatus.InProgress)
            .Select(r => new RoutePlanDto
            {
                Id = r.Id,
                DriverId = r.DriverId,
                ShipmentId = r.ShipmentId,
                OriginAddress = r.OriginAddress,
                DestinationAddress = r.DestinationAddress,
                TrafficLevel = r.TrafficLevel,
                Status = r.Status,
                TotalDistanceKm = r.TotalDistanceKm,
                EstimatedDurationMinutes = r.EstimatedDurationMinutes,
                EstimatedArrival = r.EstimatedArrival,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return new ApiResponse<List<RoutePlanDto>> { Success = true, Data = routes };
    }
}

using FedCarrier.Contracts;
using FedCarrier.Tracking.Application.Commands;
using FedCarrier.Tracking.Application.Queries;
using FedCarrier.Tracking.Domain;
using FedCarrier.Tracking.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Tracking.Application.Handlers;

public class CreateTrackingLocationCommandHandler : IRequestHandler<CreateTrackingLocationCommand, ApiResponse<Guid>>
{
    private readonly TrackingDbContext _db;
    public CreateTrackingLocationCommandHandler(TrackingDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(CreateTrackingLocationCommand request, CancellationToken ct)
    {
        var location = new TrackingLocation
        {
            Id = Guid.NewGuid(),
            ShipmentId = request.ShipmentId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            Speed = request.Speed,
            Heading = request.Heading,
            Timestamp = DateTime.UtcNow,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        _db.TrackingLocations.Add(location);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = location.Id };
    }
}

public class UpdateTrackingStatusCommandHandler : IRequestHandler<UpdateTrackingStatusCommand, ApiResponse<Unit>>
{
    private readonly TrackingDbContext _db;
    public UpdateTrackingStatusCommandHandler(TrackingDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateTrackingStatusCommand request, CancellationToken ct)
    {
        var locations = await _db.TrackingLocations
            .Where(l => l.ShipmentId == request.ShipmentId)
            .ToListAsync(ct);

        foreach (var location in locations)
        {
            location.Status = request.Status;
        }

        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class GetTrackingHistoryQueryHandler : IRequestHandler<GetTrackingHistoryQuery, ApiResponse<List<TrackingLocationDto>>>
{
    private readonly TrackingDbContext _db;
    public GetTrackingHistoryQueryHandler(TrackingDbContext db) => _db = db;

    public async Task<ApiResponse<List<TrackingLocationDto>>> Handle(GetTrackingHistoryQuery request, CancellationToken ct)
    {
        var locations = await _db.TrackingLocations
            .Where(l => l.ShipmentId == request.ShipmentId)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new TrackingLocationDto
            {
                Id = l.Id,
                ShipmentId = l.ShipmentId,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Address = l.Address,
                Speed = l.Speed,
                Heading = l.Heading,
                Timestamp = l.Timestamp,
                Status = l.Status
            })
            .ToListAsync(ct);

        return new ApiResponse<List<TrackingLocationDto>> { Success = true, Data = locations };
    }
}

public class GetCurrentTrackingQueryHandler : IRequestHandler<GetCurrentTrackingQuery, ApiResponse<TrackingLocationDto>>
{
    private readonly TrackingDbContext _db;
    public GetCurrentTrackingQueryHandler(TrackingDbContext db) => _db = db;

    public async Task<ApiResponse<TrackingLocationDto>> Handle(GetCurrentTrackingQuery request, CancellationToken ct)
    {
        var location = await _db.TrackingLocations
            .Where(l => l.ShipmentId == request.ShipmentId)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefaultAsync(ct);

        if (location is null)
            return new ApiResponse<TrackingLocationDto> { Success = false, Errors = new List<string> { "No tracking data found" } };

        return new ApiResponse<TrackingLocationDto>
        {
            Success = true,
            Data = new TrackingLocationDto
            {
                Id = location.Id,
                ShipmentId = location.ShipmentId,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Address = location.Address,
                Speed = location.Speed,
                Heading = location.Heading,
                Timestamp = location.Timestamp,
                Status = location.Status
            }
        };
    }
}



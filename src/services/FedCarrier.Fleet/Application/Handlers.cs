using FedCarrier.Contracts;
using FedCarrier.Fleet.Application.Commands;
using FedCarrier.Fleet.Application.Queries;
using FedCarrier.Fleet.Domain;
using FedCarrier.Fleet.Infrastructure;
using FedCarrier.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Fleet.Application.Handlers;

public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, ApiResponse<Guid>>
{
    private readonly FleetDbContext _db;
    public CreateVehicleCommandHandler(FleetDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(CreateVehicleCommand request, CancellationToken ct)
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            LicensePlate = request.LicensePlate,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            CapacityWeight = request.CapacityWeight,
            Status = VehicleStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = vehicle.Id };
    }
}

public class UpdateVehicleCommandHandler : IRequestHandler<UpdateVehicleCommand, ApiResponse<Unit>>
{
    private readonly FleetDbContext _db;
    public UpdateVehicleCommandHandler(FleetDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateVehicleCommand request, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.FindAsync(new object[] { request.Id }, ct);
        if (vehicle is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Vehicle not found" } };

        if (request.Make is not null) vehicle.Make = request.Make;
        if (request.Model is not null) vehicle.Model = request.Model;
        if (request.Year.HasValue) vehicle.Year = request.Year.Value;
        if (request.CapacityWeight.HasValue) vehicle.CapacityWeight = request.CapacityWeight.Value;

        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class DeleteVehicleCommandHandler : IRequestHandler<DeleteVehicleCommand, ApiResponse<Unit>>
{
    private readonly FleetDbContext _db;
    public DeleteVehicleCommandHandler(FleetDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(DeleteVehicleCommand request, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.FindAsync(new object[] { request.Id }, ct);
        if (vehicle is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Vehicle not found" } };

        vehicle.Status = VehicleStatus.Retired;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class AssignDriverCommandHandler : IRequestHandler<AssignDriverCommand, ApiResponse<Unit>>
{
    private readonly FleetDbContext _db;
    public AssignDriverCommandHandler(FleetDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(AssignDriverCommand request, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.FindAsync(new object[] { request.VehicleId }, ct);
        if (vehicle is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Vehicle not found" } };

        vehicle.AssignedDriverId = request.DriverId;
        vehicle.Status = VehicleStatus.InUse;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class GetVehicleQueryHandler : IRequestHandler<GetVehicleQuery, ApiResponse<VehicleDto>>
{
    private readonly FleetDbContext _db;
    public GetVehicleQueryHandler(FleetDbContext db) => _db = db;

    public async Task<ApiResponse<VehicleDto>> Handle(GetVehicleQuery request, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.FindAsync(new object[] { request.Id }, ct);
        if (vehicle is null)
            return new ApiResponse<VehicleDto> { Success = false, Errors = new List<string> { "Vehicle not found" } };

        return new ApiResponse<VehicleDto>
        {
            Success = true,
            Data = new VehicleDto
            {
                Id = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Status = vehicle.Status,
                CapacityWeight = vehicle.CapacityWeight,
                AssignedDriverId = vehicle.AssignedDriverId,
                CreatedAt = vehicle.CreatedAt
            }
        };
    }
}

public class GetAllVehiclesQueryHandler : IRequestHandler<GetAllVehiclesQuery, ApiResponse<PagedResult<VehicleSummaryDto>>>
{
    private readonly FleetDbContext _db;
    public GetAllVehiclesQueryHandler(FleetDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<VehicleSummaryDto>>> Handle(GetAllVehiclesQuery request, CancellationToken ct)
    {
        var query = _db.Vehicles.AsQueryable();
        if (request.Status.HasValue)
            query = query.Where(v => v.Status == request.Status.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VehicleSummaryDto
            {
                Id = v.Id,
                LicensePlate = v.LicensePlate,
                Make = v.Make,
                Model = v.Model,
                Status = v.Status
            })
            .ToListAsync(ct);

        return new ApiResponse<PagedResult<VehicleSummaryDto>>
        {
            Success = true,
            Data = new PagedResult<VehicleSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}

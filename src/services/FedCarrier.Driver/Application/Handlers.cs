using FedCarrier.Contracts;
using FedCarrier.Driver.Application.Commands;
using FedCarrier.Driver.Application.Queries;
using FedCarrier.Driver.Domain;
using FedCarrier.Driver.Infrastructure;
using FedCarrier.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Driver.Application.Handlers;

public class CreateDriverCommandHandler : IRequestHandler<CreateDriverCommand, ApiResponse<Guid>>
{
    private readonly DriverDbContext _db;
    public CreateDriverCommandHandler(DriverDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(CreateDriverCommand request, CancellationToken ct)
    {
        var driver = new FedCarrier.Driver.Domain.Driver
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            LicenseNumber = request.LicenseNumber,
            Phone = request.Phone,
            Status = DriverStatus.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = driver.Id };
    }
}

public class UpdateDriverCommandHandler : IRequestHandler<UpdateDriverCommand, ApiResponse<Unit>>
{
    private readonly DriverDbContext _db;
    public UpdateDriverCommandHandler(DriverDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateDriverCommand request, CancellationToken ct)
    {
        var driver = await _db.Drivers.FindAsync(new object[] { request.Id }, ct);
        if (driver is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Driver not found" } };

        if (request.FirstName is not null) driver.FirstName = request.FirstName;
        if (request.LastName is not null) driver.LastName = request.LastName;
        if (request.Phone is not null) driver.Phone = request.Phone;

        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class DeleteDriverCommandHandler : IRequestHandler<DeleteDriverCommand, ApiResponse<Unit>>
{
    private readonly DriverDbContext _db;
    public DeleteDriverCommandHandler(DriverDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(DeleteDriverCommand request, CancellationToken ct)
    {
        var driver = await _db.Drivers.FindAsync(new object[] { request.Id }, ct);
        if (driver is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Driver not found" } };

        driver.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class UpdateDriverLocationCommandHandler : IRequestHandler<UpdateDriverLocationCommand, ApiResponse<Unit>>
{
    private readonly DriverDbContext _db;
    public UpdateDriverLocationCommandHandler(DriverDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateDriverLocationCommand request, CancellationToken ct)
    {
        var driver = await _db.Drivers.FindAsync(new object[] { request.DriverId }, ct);
        if (driver is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Driver not found" } };

        driver.CurrentLocation = request.Location;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class UpdateDriverAvailabilityCommandHandler : IRequestHandler<UpdateDriverAvailabilityCommand, ApiResponse<Unit>>
{
    private readonly DriverDbContext _db;
    public UpdateDriverAvailabilityCommandHandler(DriverDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateDriverAvailabilityCommand request, CancellationToken ct)
    {
        var driver = await _db.Drivers.FindAsync(new object[] { request.DriverId }, ct);
        if (driver is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Driver not found" } };

        driver.Status = request.Status;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class GetDriverQueryHandler : IRequestHandler<GetDriverQuery, ApiResponse<DriverDto>>
{
    private readonly DriverDbContext _db;
    public GetDriverQueryHandler(DriverDbContext db) => _db = db;

    public async Task<ApiResponse<DriverDto>> Handle(GetDriverQuery request, CancellationToken ct)
    {
        var driver = await _db.Drivers.FindAsync(new object[] { request.Id }, ct);
        if (driver is null)
            return new ApiResponse<DriverDto> { Success = false, Errors = new List<string> { "Driver not found" } };

        return new ApiResponse<DriverDto>
        {
            Success = true,
            Data = new DriverDto
            {
                Id = driver.Id,
                FirstName = driver.FirstName,
                LastName = driver.LastName,
                LicenseNumber = driver.LicenseNumber,
                Phone = driver.Phone,
                CurrentLocation = driver.CurrentLocation,
                Status = driver.Status,
                IsActive = driver.IsActive,
                CreatedAt = driver.CreatedAt
            }
        };
    }
}

public class GetAllDriversQueryHandler : IRequestHandler<GetAllDriversQuery, ApiResponse<PagedResult<DriverSummaryDto>>>
{
    private readonly DriverDbContext _db;
    public GetAllDriversQueryHandler(DriverDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<DriverSummaryDto>>> Handle(GetAllDriversQuery request, CancellationToken ct)
    {
        var query = _db.Drivers.AsQueryable();
        if (request.Status.HasValue)
            query = query.Where(d => d.Status == request.Status.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DriverSummaryDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Status = d.Status
            })
            .ToListAsync(ct);

        return new ApiResponse<PagedResult<DriverSummaryDto>>
        {
            Success = true,
            Data = new PagedResult<DriverSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}

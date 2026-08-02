using FedCarrier.Contracts;
using FedCarrier.Shipment.Application.Commands;
using FedCarrier.Shipment.Application.Queries;
using FedCarrier.Shipment.Domain;
using FedCarrier.Shipment.Infrastructure;
using FedCarrier.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Shipment.Application.Handlers;

public class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, ApiResponse<Guid>>
{
    private readonly ShipmentDbContext _db;
    public CreateShipmentCommandHandler(ShipmentDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(CreateShipmentCommand request, CancellationToken ct)
    {
        var shipment = new FedCarrier.Shipment.Domain.Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            Origin = request.Origin,
            Destination = request.Destination,
            Status = ShipmentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            shipment.Items.Add(new ShipmentItem
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Description = item.Description,
                Weight = item.Weight,
                Quantity = item.Quantity,
                Price = item.Price
            });
        }

        shipment.StatusHistory.Add(new StatusHistory
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            Status = ShipmentStatus.Pending,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = "system"
        });

        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = shipment.Id };
    }
}

public class AssignDriverCommandHandler : IRequestHandler<AssignDriverCommand, ApiResponse<Unit>>
{
    private readonly ShipmentDbContext _db;
    public AssignDriverCommandHandler(ShipmentDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(AssignDriverCommand request, CancellationToken ct)
    {
        var shipment = await _db.Shipments.FindAsync(new object[] { request.ShipmentId }, ct);
        if (shipment is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Shipment not found" } };

        shipment.AssignedDriverId = request.DriverId;
        shipment.VehicleId = request.VehicleId;
        shipment.Status = ShipmentStatus.Assigned;
        shipment.UpdatedAt = DateTime.UtcNow;

        _db.StatusHistory.Add(new StatusHistory
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            Status = ShipmentStatus.Assigned,
            Notes = "Driver " + request.DriverId + " assigned",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = "system"
        });

        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class UpdateShipmentStatusCommandHandler : IRequestHandler<UpdateShipmentStatusCommand, ApiResponse<Unit>>
{
    private readonly ShipmentDbContext _db;
    public UpdateShipmentStatusCommandHandler(ShipmentDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateShipmentStatusCommand request, CancellationToken ct)
    {
        var shipment = await _db.Shipments.FindAsync(new object[] { request.ShipmentId }, ct);
        if (shipment is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Shipment not found" } };

        shipment.Status = request.Status;
        shipment.UpdatedAt = DateTime.UtcNow;

        _db.StatusHistory.Add(new StatusHistory
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            Status = request.Status,
            Notes = request.Notes,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = request.ChangedBy
        });

        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class GetShipmentQueryHandler : IRequestHandler<GetShipmentQuery, ApiResponse<ShipmentDto>>
{
    private readonly ShipmentDbContext _db;
    public GetShipmentQueryHandler(ShipmentDbContext db) => _db = db;

    public async Task<ApiResponse<ShipmentDto>> Handle(GetShipmentQuery request, CancellationToken ct)
    {
        var shipment = await _db.Shipments
            .Include(s => s.Items)
            .Include(s => s.StatusHistory)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (shipment is null)
            return new ApiResponse<ShipmentDto> { Success = false, Errors = new List<string> { "Shipment not found" } };

        return new ApiResponse<ShipmentDto>
        {
            Success = true,
            Data = new ShipmentDto
            {
                Id = shipment.Id,
                OrderId = shipment.OrderId,
                Origin = shipment.Origin,
                Destination = shipment.Destination,
                Status = shipment.Status,
                AssignedDriverId = shipment.AssignedDriverId,
                VehicleId = shipment.VehicleId,
                Items = shipment.Items.Select(i => new ShipmentItemDto
                {
                    Description = i.Description,
                    Weight = i.Weight,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList(),
                StatusHistory = shipment.StatusHistory.Select(h => new StatusHistoryDto
                {
                    Status = h.Status,
                    Notes = h.Notes,
                    ChangedAt = h.ChangedAt,
                    ChangedBy = h.ChangedBy
                }).ToList(),
                CreatedAt = shipment.CreatedAt,
                UpdatedAt = shipment.UpdatedAt
            }
        };
    }
}

public class SearchShipmentsQueryHandler : IRequestHandler<SearchShipmentsQuery, ApiResponse<PagedResult<ShipmentSummaryDto>>>
{
    private readonly ShipmentDbContext _db;
    public SearchShipmentsQueryHandler(ShipmentDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<ShipmentSummaryDto>>> Handle(SearchShipmentsQuery request, CancellationToken ct)
    {
        var query = _db.Shipments.AsQueryable();

        if (request.Origin is not null)
            query = query.Where(s => s.Origin.Contains(request.Origin));
        if (request.Destination is not null)
            query = query.Where(s => s.Destination.Contains(request.Destination));
        if (request.Status.HasValue)
            query = query.Where(s => s.Status == request.Status.Value);
        if (request.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ShipmentSummaryDto
            {
                Id = s.Id,
                Origin = s.Origin,
                Destination = s.Destination,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(ct);

        return new ApiResponse<PagedResult<ShipmentSummaryDto>>
        {
            Success = true,
            Data = new PagedResult<ShipmentSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}

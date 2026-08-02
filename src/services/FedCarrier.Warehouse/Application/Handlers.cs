using FedCarrier.Contracts;
using FedCarrier.Warehouse.Application.Commands;
using FedCarrier.Warehouse.Application.Queries;
using FedCarrier.Warehouse.Domain;
using FedCarrier.Warehouse.Infrastructure;
using FedCarrier.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Warehouse.Application.Handlers;

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, ApiResponse<Guid>>
{
    private readonly WarehouseDbContext _db;
    public CreateWarehouseCommandHandler(WarehouseDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = new FedCarrier.Warehouse.Domain.Warehouse
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = warehouse.Id };
    }
}

public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, ApiResponse<Unit>>
{
    private readonly WarehouseDbContext _db;
    public UpdateWarehouseCommandHandler(WarehouseDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FindAsync(new object[] { request.Id }, ct);
        if (warehouse is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Warehouse not found" } };

        if (request.Name is not null) warehouse.Name = request.Name;
        if (request.Address is not null) warehouse.Address = request.Address;
        if (request.City is not null) warehouse.City = request.City;
        if (request.State is not null) warehouse.State = request.State;
        if (request.ZipCode is not null) warehouse.ZipCode = request.ZipCode;
        if (request.Country is not null) warehouse.Country = request.Country;

        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, ApiResponse<Unit>>
{
    private readonly WarehouseDbContext _db;
    public DeleteWarehouseCommandHandler(WarehouseDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(DeleteWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FindAsync(new object[] { request.Id }, ct);
        if (warehouse is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Warehouse not found" } };

        warehouse.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class AddInventoryCommandHandler : IRequestHandler<AddInventoryCommand, ApiResponse<Guid>>
{
    private readonly WarehouseDbContext _db;
    public AddInventoryCommandHandler(WarehouseDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(AddInventoryCommand request, CancellationToken ct)
    {
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            WarehouseId = request.WarehouseId,
            ProductName = request.ProductName,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            LastUpdated = DateTime.UtcNow
        };

        _db.Inventories.Add(inventory);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = inventory.Id };
    }
}

public class GetWarehouseQueryHandler : IRequestHandler<GetWarehouseQuery, ApiResponse<WarehouseDto>>
{
    private readonly WarehouseDbContext _db;
    public GetWarehouseQueryHandler(WarehouseDbContext db) => _db = db;

    public async Task<ApiResponse<WarehouseDto>> Handle(GetWarehouseQuery request, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FindAsync(new object[] { request.Id }, ct);
        if (warehouse is null)
            return new ApiResponse<WarehouseDto> { Success = false, Errors = new List<string> { "Warehouse not found" } };

        return new ApiResponse<WarehouseDto>
        {
            Success = true,
            Data = new WarehouseDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Address = warehouse.Address,
                City = warehouse.City,
                State = warehouse.State,
                ZipCode = warehouse.ZipCode,
                Country = warehouse.Country,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt
            }
        };
    }
}

public class GetAllWarehousesQueryHandler : IRequestHandler<GetAllWarehousesQuery, ApiResponse<PagedResult<WarehouseSummaryDto>>>
{
    private readonly WarehouseDbContext _db;
    public GetAllWarehousesQueryHandler(WarehouseDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<WarehouseSummaryDto>>> Handle(GetAllWarehousesQuery request, CancellationToken ct)
    {
        var query = _db.Warehouses.AsQueryable();
        if (request.City is not null)
            query = query.Where(w => w.City.Contains(request.City));
        if (request.IsActive.HasValue)
            query = query.Where(w => w.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(w => new WarehouseSummaryDto
            {
                Id = w.Id,
                Name = w.Name,
                City = w.City,
                IsActive = w.IsActive
            })
            .ToListAsync(ct);

        return new ApiResponse<PagedResult<WarehouseSummaryDto>>
        {
            Success = true,
            Data = new PagedResult<WarehouseSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}

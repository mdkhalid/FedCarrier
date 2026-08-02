using FedCarrier.Contracts;
using MediatR;

namespace FedCarrier.Warehouse.Application.Queries;

public class GetWarehouseQuery : IRequest<ApiResponse<WarehouseDto>>
{
    public Guid Id { get; set; }
}

public class GetAllWarehousesQuery : IRequest<ApiResponse<PagedResult<WarehouseSummaryDto>>>
{
    public string? City { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WarehouseSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Fleet.Domain;

namespace FedCarrier.Fleet.Application.Queries;

public class GetVehicleQuery : IRequest<ApiResponse<VehicleDto>>
{
    public Guid Id { get; set; }
}

public class GetAllVehiclesQuery : IRequest<ApiResponse<PagedResult<VehicleSummaryDto>>>
{
    public VehicleStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class VehicleDto
{
    public Guid Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public VehicleStatus Status { get; set; }
    public decimal CapacityWeight { get; set; }
    public Guid? AssignedDriverId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VehicleSummaryDto
{
    public Guid Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public VehicleStatus Status { get; set; }
}

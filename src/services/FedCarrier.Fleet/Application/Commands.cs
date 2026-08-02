using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Fleet.Domain;

namespace FedCarrier.Fleet.Application.Commands;

public class CreateVehicleCommand : IRequest<ApiResponse<Guid>>
{
    public string LicensePlate { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal CapacityWeight { get; set; }
}

public class UpdateVehicleCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public decimal? CapacityWeight { get; set; }
}

public class DeleteVehicleCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
}

public class AssignDriverCommand : IRequest<ApiResponse<Unit>>
{
    public Guid VehicleId { get; set; }
    public Guid DriverId { get; set; }
}

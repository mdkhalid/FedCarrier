using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Driver.Domain;

namespace FedCarrier.Driver.Application.Commands;

public class CreateDriverCommand : IRequest<ApiResponse<Guid>>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class UpdateDriverCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
}

public class DeleteDriverCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
}

public class UpdateDriverLocationCommand : IRequest<ApiResponse<Unit>>
{
    public Guid DriverId { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class UpdateDriverAvailabilityCommand : IRequest<ApiResponse<Unit>>
{
    public Guid DriverId { get; set; }
    public DriverStatus Status { get; set; }
}

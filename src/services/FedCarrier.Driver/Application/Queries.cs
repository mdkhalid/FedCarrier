using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Driver.Domain;

namespace FedCarrier.Driver.Application.Queries;

public class GetDriverQuery : IRequest<ApiResponse<DriverDto>>
{
    public Guid Id { get; set; }
}

public class GetAllDriversQuery : IRequest<ApiResponse<PagedResult<DriverSummaryDto>>>
{
    public DriverStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DriverDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CurrentLocation { get; set; } = string.Empty;
    public DriverStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DriverSummaryDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DriverStatus Status { get; set; }
}

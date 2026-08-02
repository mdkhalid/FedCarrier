using FedCarrier.Contracts;
using MediatR;

namespace FedCarrier.Customer.Application.Commands;

public class CreateCustomerCommand : IRequest<ApiResponse<Guid>>
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class UpdateCustomerCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class DeleteCustomerCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
}

public class AddAddressCommand : IRequest<ApiResponse<Guid>>
{
    public Guid CustomerId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

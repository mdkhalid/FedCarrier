using FedCarrier.Contracts;
using MediatR;

namespace FedCarrier.Customer.Application.Queries;

public class GetCustomerQuery : IRequest<ApiResponse<CustomerDto>>
{
    public Guid Id { get; set; }
}

public class GetAllCustomersQuery : IRequest<ApiResponse<PagedResult<CustomerSummaryDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetCustomerAddressesQuery : IRequest<ApiResponse<List<AddressDto>>>
{
    public Guid CustomerId { get; set; }
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerSummaryDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class AddressDto
{
    public Guid Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

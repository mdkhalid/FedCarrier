using FedCarrier.Contracts;
using FedCarrier.Customer.Application.Commands;
using FedCarrier.Customer.Application.Queries;
using FedCarrier.Customer.Domain;
using FedCarrier.Customer.Infrastructure;
using FedCarrier.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Customer.Application.Handlers;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, ApiResponse<Guid>>
{
    private readonly CustomerDbContext _db;
    public CreateCustomerCommandHandler(CustomerDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var customer = new FedCarrier.Customer.Domain.Customer
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Address = request.Address,
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = customer.Id };
    }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, ApiResponse<Unit>>
{
    private readonly CustomerDbContext _db;
    public UpdateCustomerCommandHandler(CustomerDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var customer = await _db.Customers.FindAsync(new object[] { request.Id }, ct);
        if (customer is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Customer not found" } };

        if (request.FirstName is not null) customer.FirstName = request.FirstName;
        if (request.LastName is not null) customer.LastName = request.LastName;
        if (request.Phone is not null) customer.Phone = request.Phone;
        if (request.Address is not null) customer.Address = request.Address;

        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, ApiResponse<Unit>>
{
    private readonly CustomerDbContext _db;
    public DeleteCustomerCommandHandler(CustomerDbContext db) => _db = db;

    public async Task<ApiResponse<Unit>> Handle(DeleteCustomerCommand request, CancellationToken ct)
    {
        var customer = await _db.Customers.FindAsync(new object[] { request.Id }, ct);
        if (customer is null)
            return new ApiResponse<Unit> { Success = false, Errors = new List<string> { "Customer not found" } };

        customer.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return new ApiResponse<Unit> { Success = true, Data = Unit.Value };
    }
}

public class AddAddressCommandHandler : IRequestHandler<AddAddressCommand, ApiResponse<Guid>>
{
    private readonly CustomerDbContext _db;
    public AddAddressCommandHandler(CustomerDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(AddAddressCommand request, CancellationToken ct)
    {
        var address = new Address
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Street = request.Street,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            IsDefault = request.IsDefault
        };

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = address.Id };
    }
}

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, ApiResponse<CustomerDto>>
{
    private readonly CustomerDbContext _db;
    public GetCustomerQueryHandler(CustomerDbContext db) => _db = db;

    public async Task<ApiResponse<CustomerDto>> Handle(GetCustomerQuery request, CancellationToken ct)
    {
        var customer = await _db.Customers.FindAsync(new object[] { request.Id }, ct);
        if (customer is null)
            return new ApiResponse<CustomerDto> { Success = false, Errors = new List<string> { "Customer not found" } };

        return new ApiResponse<CustomerDto>
        {
            Success = true,
            Data = new CustomerDto
            {
                Id = customer.Id,
                Email = customer.Email,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Phone = customer.Phone,
                Address = customer.Address,
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedAt
            }
        };
    }
}

public class GetAllCustomersQueryHandler : IRequestHandler<GetAllCustomersQuery, ApiResponse<PagedResult<CustomerSummaryDto>>>
{
    private readonly CustomerDbContext _db;
    public GetAllCustomersQueryHandler(CustomerDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<CustomerSummaryDto>>> Handle(GetAllCustomersQuery request, CancellationToken ct)
    {
        var query = _db.Customers.AsQueryable();
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerSummaryDto
            {
                Id = c.Id,
                Email = c.Email,
                FirstName = c.FirstName,
                LastName = c.LastName
            })
            .ToListAsync(ct);

        return new ApiResponse<PagedResult<CustomerSummaryDto>>
        {
            Success = true,
            Data = new PagedResult<CustomerSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}

public class GetCustomerAddressesQueryHandler : IRequestHandler<GetCustomerAddressesQuery, ApiResponse<List<AddressDto>>>
{
    private readonly CustomerDbContext _db;
    public GetCustomerAddressesQueryHandler(CustomerDbContext db) => _db = db;

    public async Task<ApiResponse<List<AddressDto>>> Handle(GetCustomerAddressesQuery request, CancellationToken ct)
    {
        var addresses = await _db.Addresses
            .Where(a => a.CustomerId == request.CustomerId)
            .Select(a => new AddressDto
            {
                Id = a.Id,
                Street = a.Street,
                City = a.City,
                State = a.State,
                ZipCode = a.ZipCode,
                Country = a.Country,
                IsDefault = a.IsDefault
            })
            .ToListAsync(ct);

        return new ApiResponse<List<AddressDto>> { Success = true, Data = addresses };
    }
}

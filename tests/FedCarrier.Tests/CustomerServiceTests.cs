using FedCarrier.Contracts;
using FedCarrier.Customer.Application.Commands;
using FedCarrier.Customer.Application.Handlers;
using FedCarrier.Customer.Application.Queries;
using FedCarrier.Customer.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class CustomerServiceTests
{
    private CustomerDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CustomerDbContext(options);
    }

    [Fact]
    public async Task CreateCustomerCommandHandler_ShouldCreateCustomer()
    {
        var db = GetDbContext();
        var handler = new CreateCustomerCommandHandler(db);
        var command = new CreateCustomerCommand
        {
            Email = "customer@fedcarrier.com",
            FirstName = "Alice",
            LastName = "Smith",
            Phone = "01000000000",
            Address = "Cairo"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCustomerQueryHandler_ShouldReturnCustomer()
    {
        var db = GetDbContext();
        var handler = new CreateCustomerCommandHandler(db);
        var command = new CreateCustomerCommand
        {
            Email = "customer@fedcarrier.com",
            FirstName = "Alice",
            LastName = "Smith"
        };
        var createResult = await handler.Handle(command, CancellationToken.None);

        var getHandler = new GetCustomerQueryHandler(db);
        var query = new GetCustomerQuery { Id = createResult.Data };

        var result = await getHandler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Email.Should().Be("customer@fedcarrier.com");
        result.Data.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task AddAddressCommandHandler_ShouldCreateAddress()
    {
        var db = GetDbContext();
        var createHandler = new CreateCustomerCommandHandler(db);
        var customerId = (await createHandler.Handle(new CreateCustomerCommand
        {
            Email = "addr@fedcarrier.com",
            FirstName = "Bob",
            LastName = "Brown"
        }, CancellationToken.None)).Data;

        var handler = new AddAddressCommandHandler(db);
        var command = new AddAddressCommand
        {
            CustomerId = customerId,
            Street = "Main St",
            City = "Cairo",
            State = "Cairo",
            ZipCode = "11511",
            Country = "Egypt",
            IsDefault = true
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var addresses = await db.Addresses.Where(a => a.CustomerId == customerId).ToListAsync();
        addresses.Should().HaveCount(1);
        addresses[0].Street.Should().Be("Main St");
    }

    [Fact]
    public async Task GetAllCustomersQueryHandler_ShouldReturnPagedResult()
    {
        var db = GetDbContext();
        var handler = new CreateCustomerCommandHandler(db);
        await handler.Handle(new CreateCustomerCommand { Email = "a@fedcarrier.com", FirstName = "A", LastName = "A" }, CancellationToken.None);
        await handler.Handle(new CreateCustomerCommand { Email = "b@fedcarrier.com", FirstName = "B", LastName = "B" }, CancellationToken.None);

        var getHandler = new GetAllCustomersQueryHandler(db);
        var query = new GetAllCustomersQuery { Page = 1, PageSize = 20 };

        var result = await getHandler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
    }
}

using FedCarrier.Contracts;
using FedCarrier.Billing.Application.Commands;
using FedCarrier.Billing.Application.Handlers;
using FedCarrier.Billing.Application.Queries;
using FedCarrier.Billing.Domain;
using FedCarrier.Billing.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class BillingServiceTests
{
    private BillingDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    [Fact]
    public async Task CreateInvoiceCommandHandler_ShouldCreateInvoiceWithTotals()
    {
        var db = GetDbContext();
        var handler = new CreateInvoiceCommandHandler(db);
        var command = new CreateInvoiceCommand
        {
            ShipmentId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 1000m,
            TaxRate = 0.1m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Items = new List<CreateInvoiceItemDto>
            {
                new CreateInvoiceItemDto { Description = "Freight", Quantity = 1, UnitPrice = 1000m }
            }
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var invoice = await db.Invoices.Include(i => i.Items).FirstAsync(i => i.Id == result.Data);
        invoice.TaxAmount.Should().Be(100m);
        invoice.TotalAmount.Should().Be(1100m);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.InvoiceNumber.Should().StartWith("INV-");
        invoice.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ConfirmPaymentCommandHandler_ShouldMarkInvoicePaid()
    {
        var db = GetDbContext();
        var createHandler = new CreateInvoiceCommandHandler(db);
        var invoiceId = (await createHandler.Handle(new CreateInvoiceCommand
        {
            ShipmentId = Guid.NewGuid(),
            Amount = 500m,
            DueDate = DateTime.UtcNow.AddDays(15),
            Items = new List<CreateInvoiceItemDto>
            {
                new CreateInvoiceItemDto { Description = "Delivery", Quantity = 1, UnitPrice = 500m }
            }
        }, CancellationToken.None)).Data;

        var handler = new ConfirmPaymentCommandHandler(db);
        var command = new ConfirmPaymentCommand { InvoiceId = invoiceId };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var invoice = await db.Invoices.FindAsync(invoiceId);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetInvoiceQueryHandler_ShouldReturnInvoice()
    {
        var db = GetDbContext();
        var createHandler = new CreateInvoiceCommandHandler(db);
        var invoiceId = (await createHandler.Handle(new CreateInvoiceCommand
        {
            ShipmentId = Guid.NewGuid(),
            Amount = 250m,
            DueDate = DateTime.UtcNow.AddDays(20),
            Items = new List<CreateInvoiceItemDto>
            {
                new CreateInvoiceItemDto { Description = "Express", Quantity = 1, UnitPrice = 250m }
            }
        }, CancellationToken.None)).Data;

        var handler = new GetInvoiceQueryHandler(db);
        var query = new GetInvoiceQuery { Id = invoiceId };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.TotalAmount.Should().Be(250m);
        result.Data.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchInvoicesQueryHandler_ShouldFilterByStatus()
    {
        var db = GetDbContext();
        var createHandler = new CreateInvoiceCommandHandler(db);
        await createHandler.Handle(new CreateInvoiceCommand
        {
            ShipmentId = Guid.NewGuid(),
            Amount = 100m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Items = new List<CreateInvoiceItemDto>
            {
                new CreateInvoiceItemDto { Description = "Freight", Quantity = 1, UnitPrice = 100m }
            }
        }, CancellationToken.None);

        var handler = new SearchInvoicesQueryHandler(db);
        var query = new SearchInvoicesQuery { Status = InvoiceStatus.Draft, Page = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.TotalCount.Should().Be(1);
    }
}

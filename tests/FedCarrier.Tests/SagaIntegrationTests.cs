using FedCarrier.Contracts;
using FedCarrier.Infrastructure;
using FedCarrier.Saga.Application;
using FedCarrier.Saga.Domain;
using FedCarrier.Saga.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FedCarrier.Tests;

public class SagaIntegrationTests
{
    private SagaDbContext GetDbContext(string? name = null)
    {
        var options = new DbContextOptionsBuilder<SagaDbContext>()
            .UseInMemoryDatabase(databaseName: name ?? Guid.NewGuid().ToString())
            .Options;
        return new SagaDbContext(options);
    }

    private static (SagaOrchestrator orchestrator, List<IntegrationEvent> published, IEventBus bus)
        CreateOrchestrator(SagaDbContext db)
    {
        var published = new List<IntegrationEvent>();
        var bus = new RecordingEventBus(published);
        var orchestrator = new SagaOrchestrator(db, bus, NullLogger<SagaOrchestrator>.Instance);
        return (orchestrator, published, bus);
    }

    [Fact]
    public async Task OrderPlacedEvent_ShouldInitiateSaga_AndPublishCreateShipmentCommand()
    {
        var db = GetDbContext();
        var (orchestrator, published, _) = CreateOrchestrator(db);
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();

        await orchestrator.HandleOrderPlacedAsync(new OrderPlacedEvent
        {
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Ahmed",
            TotalAmount = 250m,
            CorrelationId = correlationId
        });

        var saga = await db.SagaStates.SingleAsync(s => s.OrderId == orderId);
        saga.Status.Should().Be(SagaStatus.InProgress);
        saga.CurrentStep.Should().Be(SagaStep.Initiated);

        published.Should().ContainSingle()
            .Which.Should().BeOfType<CreateShipmentCommandEvent>()
            .Which.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task OrderPlacedEvent_ShouldBeIdempotent()
    {
        var db = GetDbContext();
        var (orchestrator, _, _) = CreateOrchestrator(db);
        var orderId = Guid.NewGuid();

        await orchestrator.HandleOrderPlacedAsync(new OrderPlacedEvent { OrderId = orderId, CustomerId = Guid.NewGuid() });
        await orchestrator.HandleOrderPlacedAsync(new OrderPlacedEvent { OrderId = orderId, CustomerId = Guid.NewGuid() });

        (await db.SagaStates.CountAsync(s => s.OrderId == orderId)).Should().Be(1);
    }

    [Fact]
    public async Task ShipmentCreatedAndDelivered_ShouldPublishCreateInvoiceCommand()
    {
        var db = GetDbContext();
        var (orchestrator, published, _) = CreateOrchestrator(db);
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        await orchestrator.HandleOrderPlacedAsync(new OrderPlacedEvent { OrderId = orderId, CustomerId = customerId });
        await orchestrator.HandleShipmentCreatedAsync(new ShipmentCreatedEvent { ShipmentId = shipmentId, OrderId = orderId });
        await orchestrator.HandleShipmentDeliveredAsync(new ShipmentDeliveredEvent { ShipmentId = shipmentId, OrderId = orderId });

        var saga = await db.SagaStates.SingleAsync(s => s.OrderId == orderId);
        saga.ShipmentId.Should().Be(shipmentId);
        saga.CurrentStep.Should().Be(SagaStep.ShipmentDelivered);

        published.Should().Contain(x => x is CreateInvoiceCommandEvent)
            .Which.Should().BeOfType<CreateInvoiceCommandEvent>()
            .Which.ShipmentId.Should().Be(shipmentId);
    }

    [Fact]
    public async Task FullHappyPath_ShouldCompleteSaga_AndNotifyCustomer()
    {
        var db = GetDbContext();
        var (orchestrator, published, _) = CreateOrchestrator(db);
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        await orchestrator.HandleOrderPlacedAsync(new OrderPlacedEvent { OrderId = orderId, CustomerId = customerId });
        await orchestrator.HandleShipmentCreatedAsync(new ShipmentCreatedEvent { ShipmentId = shipmentId, OrderId = orderId });
        await orchestrator.HandleShipmentDeliveredAsync(new ShipmentDeliveredEvent { ShipmentId = shipmentId, OrderId = orderId });
        await orchestrator.HandleInvoiceGeneratedAsync(new InvoiceGeneratedEvent { InvoiceId = invoiceId, ShipmentId = shipmentId, TotalAmount = 275m });
        await orchestrator.HandlePaymentConfirmedAsync(new PaymentConfirmedEvent { InvoiceId = invoiceId, ShipmentId = shipmentId });

        var saga = await db.SagaStates.SingleAsync(s => s.OrderId == orderId);
        saga.Status.Should().Be(SagaStatus.Completed);
        saga.CurrentStep.Should().Be(SagaStep.PaymentConfirmed);
        saga.CompletedAt.Should().NotBeNull();

        published.Should().Contain(x => x is NotifyCustomerCommandEvent)
            .Which.Should().BeOfType<NotifyCustomerCommandEvent>()
            .Which.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task OrderCancelled_ShouldCancelSaga_AndPublishCompensation()
    {
        var db = GetDbContext();
        var (orchestrator, published, _) = CreateOrchestrator(db);
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        await orchestrator.HandleOrderPlacedAsync(new OrderPlacedEvent { OrderId = orderId, CustomerId = Guid.NewGuid() });
        await orchestrator.HandleShipmentCreatedAsync(new ShipmentCreatedEvent { ShipmentId = shipmentId, OrderId = orderId });
        await orchestrator.HandleOrderCancelledAsync(new OrderStatusChangedEvent { OrderId = orderId, Status = "Cancelled" });

        var saga = await db.SagaStates.SingleAsync(s => s.OrderId == orderId);
        saga.Status.Should().Be(SagaStatus.Cancelled);
        saga.FailureReason.Should().NotBeNullOrEmpty();

        published.Should().Contain(x => x is CancelShipmentCommandEvent)
            .Which.Should().BeOfType<CancelShipmentCommandEvent>()
            .Which.OrderId.Should().Be(orderId);
    }
}

public class RecordingEventBus : IEventBus
{
    private readonly List<IntegrationEvent> _published;

    public RecordingEventBus(List<IntegrationEvent> published) => _published = published;

    public Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        _published.Add(@event);
        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
        => PublishAsync(@event, "", cancellationToken);

    public Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, string? queue = null, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
        => Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

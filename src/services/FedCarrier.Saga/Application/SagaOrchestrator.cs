using FedCarrier.Contracts;
using FedCarrier.Infrastructure;
using FedCarrier.Saga.Domain;
using FedCarrier.Saga.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FedCarrier.Saga.Application;

public class SagaOrchestrator
{
    private readonly SagaDbContext _db;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SagaOrchestrator> _logger;

    public SagaOrchestrator(SagaDbContext db, IEventBus eventBus, ILogger<SagaOrchestrator> logger)
    {
        _db = db;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task HandleOrderPlacedAsync(OrderPlacedEvent @event, CancellationToken ct = default)
    {
        if (await _db.SagaStates.AnyAsync(s => s.OrderId == @event.OrderId, ct))
            return;

        var saga = new SagaState
        {
            Id = Guid.NewGuid(),
            OrderId = @event.OrderId,
            CustomerId = @event.CustomerId,
            CorrelationId = @event.CorrelationId,
            CurrentStep = SagaStep.Initiated,
            Status = SagaStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        _db.SagaStates.Add(saga);
        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new CreateShipmentCommandEvent
        {
            OrderId = @event.OrderId,
            CustomerId = @event.CustomerId,
            CorrelationId = @event.CorrelationId
        }, ct);

        _logger.LogInformation("Saga {SagaId} initiated for order {OrderId}", saga.Id, @event.OrderId);
    }

    public async Task HandleShipmentCreatedAsync(ShipmentCreatedEvent @event, CancellationToken ct = default)
    {
        var saga = await _db.SagaStates.FirstOrDefaultAsync(s => s.OrderId == @event.OrderId, ct);
        if (saga is null || saga.Status != SagaStatus.InProgress)
            return;

        saga.ShipmentId = @event.ShipmentId;
        saga.CurrentStep = SagaStep.ShipmentCreated;
        saga.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Saga {SagaId}: shipment {ShipmentId} created for order {OrderId}", saga.Id, @event.ShipmentId, @event.OrderId);
    }

    public async Task HandleShipmentDeliveredAsync(ShipmentDeliveredEvent @event, CancellationToken ct = default)
    {
        var saga = await _db.SagaStates.FirstOrDefaultAsync(s => s.OrderId == @event.OrderId, ct);
        if (saga is null || saga.Status != SagaStatus.InProgress)
            return;

        saga.ShipmentId = @event.ShipmentId;
        saga.CurrentStep = SagaStep.ShipmentDelivered;
        saga.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new CreateInvoiceCommandEvent
        {
            OrderId = @event.OrderId,
            ShipmentId = @event.ShipmentId,
            CustomerId = saga.CustomerId,
            Amount = 0m,
            CorrelationId = saga.CorrelationId
        }, ct);

        _logger.LogInformation("Saga {SagaId}: shipment {ShipmentId} delivered; requesting invoice", saga.Id, @event.ShipmentId);
    }

    public async Task HandleInvoiceGeneratedAsync(InvoiceGeneratedEvent @event, CancellationToken ct = default)
    {
        var saga = await _db.SagaStates.FirstOrDefaultAsync(s => s.ShipmentId == @event.ShipmentId, ct);
        if (saga is null || saga.Status != SagaStatus.InProgress)
            return;

        saga.InvoiceId = @event.InvoiceId;
        saga.CurrentStep = SagaStep.InvoiceGenerated;
        saga.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Saga {SagaId}: invoice {InvoiceId} generated for shipment {ShipmentId}", saga.Id, @event.InvoiceId, @event.ShipmentId);
    }

    public async Task HandlePaymentConfirmedAsync(PaymentConfirmedEvent @event, CancellationToken ct = default)
    {
        var saga = await _db.SagaStates.FirstOrDefaultAsync(s => s.InvoiceId == @event.InvoiceId, ct);
        if (saga is null || saga.Status != SagaStatus.InProgress)
            return;

        saga.InvoiceId = @event.InvoiceId;
        saga.CurrentStep = SagaStep.PaymentConfirmed;
        saga.Status = SagaStatus.Completed;
        saga.UpdatedAt = DateTime.UtcNow;
        saga.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new NotifyCustomerCommandEvent
        {
            CustomerId = saga.CustomerId,
            Title = "Order " + saga.OrderId + " completed",
            Message = "Your order has been fulfilled. Payment confirmed.",
            Channel = "Email",
            CorrelationId = saga.CorrelationId
        }, ct);

        _logger.LogInformation("Saga {SagaId} completed for order {OrderId}", saga.Id, saga.OrderId);
    }

    public async Task HandleOrderCancelledAsync(OrderStatusChangedEvent @event, CancellationToken ct = default)
    {
        if (!string.Equals(@event.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return;

        var saga = await _db.SagaStates.FirstOrDefaultAsync(s => s.OrderId == @event.OrderId, ct);
        if (saga is null || saga.Status != SagaStatus.InProgress)
            return;

        saga.Status = SagaStatus.Cancelled;
        saga.FailureReason = "Order cancelled by customer";
        saga.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new CancelShipmentCommandEvent
        {
            OrderId = @event.OrderId,
            CorrelationId = saga.CorrelationId
        }, ct);

        _logger.LogInformation("Saga {SagaId} cancelled for order {OrderId}; compensating shipment", saga.Id, @event.OrderId);
    }
}

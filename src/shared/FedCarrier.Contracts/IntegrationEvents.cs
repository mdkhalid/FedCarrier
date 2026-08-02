namespace FedCarrier.Contracts;

public abstract class IntegrationEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
}

public class OrderPlacedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class OrderStatusChangedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ShipmentCreatedEvent : IntegrationEvent
{
    public Guid ShipmentId { get; set; }
    public Guid OrderId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
}

public class ShipmentAssignedEvent : IntegrationEvent
{
    public Guid ShipmentId { get; set; }
    public Guid DriverId { get; set; }
    public Guid VehicleId { get; set; }
}

public class ShipmentStatusChangedEvent : IntegrationEvent
{
    public Guid ShipmentId { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ShipmentDeliveredEvent : IntegrationEvent
{
    public Guid ShipmentId { get; set; }
    public Guid OrderId { get; set; }
}

public class InvoiceGeneratedEvent : IntegrationEvent
{
    public Guid InvoiceId { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid? CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class PaymentConfirmedEvent : IntegrationEvent
{
    public Guid InvoiceId { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
}

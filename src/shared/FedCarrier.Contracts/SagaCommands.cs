namespace FedCarrier.Contracts;

public class CreateShipmentCommandEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
}

public class CancelShipmentCommandEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
}

public class CreateInvoiceCommandEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
}

public class NotifyCustomerCommandEvent : IntegrationEvent
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = "Email";
}

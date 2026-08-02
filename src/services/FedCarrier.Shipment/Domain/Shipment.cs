namespace FedCarrier.Shipment.Domain;

public enum ShipmentStatus
{
    Pending = 0,
    Assigned = 1,
    InTransit = 2,
    Delivered = 3,
    Cancelled = 4
}

public class Shipment : BaseEntity
{
    public Guid OrderId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
    public Guid? AssignedDriverId { get; set; }
    public Guid? VehicleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ShipmentItem> Items { get; set; } = new();
    public List<StatusHistory> StatusHistory { get; set; } = new();
}

public class ShipmentItem : BaseEntity
{
    public Guid ShipmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class StatusHistory : BaseEntity
{
    public Guid ShipmentId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}

public class BaseEntity
{
    public Guid Id { get; set; }
}

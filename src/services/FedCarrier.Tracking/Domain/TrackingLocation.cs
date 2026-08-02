namespace FedCarrier.Tracking.Domain;

public enum TrackingStatus
{
    PickedUp = 0,
    InTransit = 1,
    OutForDelivery = 2,
    Delivered = 3,
    Delayed = 4
}

public class TrackingLocation
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public string? Speed { get; set; }
    public string? Heading { get; set; }
    public DateTime Timestamp { get; set; }
    public TrackingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}



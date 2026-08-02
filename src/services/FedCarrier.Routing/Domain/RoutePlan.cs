namespace FedCarrier.Routing.Domain;

public enum RouteStatus
{
    Draft = 0,
    Optimized = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum TrafficLevel
{
    Low = 0,
    Moderate = 1,
    Heavy = 2,
    Severe = 3
}

public class RoutePlan
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public Guid? ShipmentId { get; set; }
    public string? OriginAddress { get; set; }
    public string? DestinationAddress { get; set; }
    public TrafficLevel TrafficLevel { get; set; }
    public RouteStatus Status { get; set; }
    public double TotalDistanceKm { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<RouteStop> Stops { get; set; } = new();
}

public class RouteStop
{
    public Guid Id { get; set; }
    public Guid RoutePlanId { get; set; }
    public int Sequence { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public DateTime? ActualArrival { get; set; }
}

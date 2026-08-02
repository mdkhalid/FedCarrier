using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Routing.Domain;

namespace FedCarrier.Routing.Application.Queries;

public class GetRoutePlanQuery : IRequest<ApiResponse<RoutePlanDto>>
{
    public Guid Id { get; set; }
}

public class GetActiveRoutesQuery : IRequest<ApiResponse<List<RoutePlanDto>>>
{
    public Guid DriverId { get; set; }
}

public class RouteStopDto
{
    public Guid Id { get; set; }
    public int Sequence { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public DateTime? ActualArrival { get; set; }
}

public class RoutePlanDto
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
    public List<RouteStopDto> Stops { get; set; } = new();
}

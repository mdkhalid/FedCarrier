using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Routing.Domain;

namespace FedCarrier.Routing.Application.Commands;

public class RouteStopInput
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
}

public class OptimizeRouteCommand : IRequest<ApiResponse<Guid>>
{
    public Guid DriverId { get; set; }
    public Guid? ShipmentId { get; set; }
    public double OriginLatitude { get; set; }
    public double OriginLongitude { get; set; }
    public string? OriginAddress { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public string? DestinationAddress { get; set; }
    public List<RouteStopInput>? Waypoints { get; set; }
    public TrafficLevel TrafficLevel { get; set; }
}

public class UpdateRouteStatusCommand : IRequest<ApiResponse<Unit>>
{
    public Guid RoutePlanId { get; set; }
    public RouteStatus Status { get; set; }
}

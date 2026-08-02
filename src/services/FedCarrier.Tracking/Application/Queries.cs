using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Tracking.Domain;

namespace FedCarrier.Tracking.Application.Queries;

public class GetTrackingHistoryQuery : IRequest<ApiResponse<List<TrackingLocationDto>>>
{
    public Guid ShipmentId { get; set; }
}

public class GetCurrentTrackingQuery : IRequest<ApiResponse<TrackingLocationDto>>
{
    public Guid ShipmentId { get; set; }
}

public class TrackingLocationDto
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
}



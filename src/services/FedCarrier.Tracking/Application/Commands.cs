using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Tracking.Domain;

namespace FedCarrier.Tracking.Application.Commands;

public class CreateTrackingLocationCommand : IRequest<ApiResponse<Guid>>
{
    public Guid ShipmentId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public string? Speed { get; set; }
    public string? Heading { get; set; }
    public TrackingStatus Status { get; set; }
}

public class UpdateTrackingStatusCommand : IRequest<ApiResponse<Unit>>
{
    public Guid ShipmentId { get; set; }
    public TrackingStatus Status { get; set; }
}



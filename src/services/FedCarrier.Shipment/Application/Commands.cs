using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Shipment.Domain;

namespace FedCarrier.Shipment.Application.Commands;

public class CreateShipmentCommand : IRequest<ApiResponse<Guid>>
{
    public Guid OrderId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public List<CreateShipmentItemDto> Items { get; set; } = new();
}

public class CreateShipmentItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class AssignDriverCommand : IRequest<ApiResponse<Unit>>
{
    public Guid ShipmentId { get; set; }
    public Guid DriverId { get; set; }
    public Guid VehicleId { get; set; }
}

public class UpdateShipmentStatusCommand : IRequest<ApiResponse<Unit>>
{
    public Guid ShipmentId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
}

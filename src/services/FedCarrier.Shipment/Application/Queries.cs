using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Shipment.Domain;

namespace FedCarrier.Shipment.Application.Queries;

public class GetShipmentQuery : IRequest<ApiResponse<ShipmentDto>>
{
    public Guid Id { get; set; }
}

public class SearchShipmentsQuery : IRequest<ApiResponse<PagedResult<ShipmentSummaryDto>>>
{
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public ShipmentStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ShipmentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; }
    public Guid? AssignedDriverId { get; set; }
    public Guid? VehicleId { get; set; }
    public List<ShipmentItemDto> Items { get; set; } = new();
    public List<StatusHistoryDto> StatusHistory { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ShipmentItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class StatusHistoryDto
{
    public ShipmentStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}

public class ShipmentSummaryDto
{
    public Guid Id { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

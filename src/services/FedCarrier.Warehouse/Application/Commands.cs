using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Warehouse.Domain;

namespace FedCarrier.Warehouse.Application.Commands;

public class CreateWarehouseCommand : IRequest<ApiResponse<Guid>>
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class UpdateWarehouseCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
}

public class DeleteWarehouseCommand : IRequest<ApiResponse<Unit>>
{
    public Guid Id { get; set; }
}

public class AddInventoryCommand : IRequest<ApiResponse<Guid>>
{
    public Guid WarehouseId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

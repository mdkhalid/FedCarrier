namespace FedCarrier.Fleet.Domain;

public enum VehicleStatus
{
    Available = 0,
    InUse = 1,
    Maintenance = 2,
    Retired = 3
}

public class Vehicle : BaseEntity
{
    public string LicensePlate { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;
    public decimal CapacityWeight { get; set; }
    public Guid? AssignedDriverId { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BaseEntity
{
    public Guid Id { get; set; }
}

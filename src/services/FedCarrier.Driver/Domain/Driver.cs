namespace FedCarrier.Driver.Domain;

public enum DriverStatus
{
    Available = 0,
    OnDuty = 1,
    OffDuty = 2,
    Unavailable = 3
}

public class Driver : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CurrentLocation { get; set; } = string.Empty;
    public DriverStatus Status { get; set; } = DriverStatus.Available;
    public DateTime? LastLocationUpdate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class BaseEntity
{
    public Guid Id { get; set; }
}

namespace FedCarrier.Reporting.Domain;

public enum ReportType
{
    Shipments = 0,
    Financial = 1,
    DriverPerformance = 2,
    Custom = 3
}

public class Report
{
    public Guid Id { get; set; }
    public ReportType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Filters { get; set; }
    public string? Data { get; set; }
    public int RowCount { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Guid? GeneratedBy { get; set; }
}

public class ReportDefinition
{
    public Guid Id { get; set; }
    public ReportType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string QueryTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

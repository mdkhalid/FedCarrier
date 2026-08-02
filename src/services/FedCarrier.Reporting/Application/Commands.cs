using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Reporting.Domain;

namespace FedCarrier.Reporting.Application.Commands;

public class GenerateReportCommand : IRequest<ApiResponse<Guid>>
{
    public ReportType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Filters { get; set; }
    public Guid? GeneratedBy { get; set; }
}

public class CreateReportDefinitionCommand : IRequest<ApiResponse<Guid>>
{
    public ReportType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string QueryTemplate { get; set; } = string.Empty;
}

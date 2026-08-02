using FedCarrier.Contracts;
using MediatR;
using FedCarrier.Reporting.Domain;

namespace FedCarrier.Reporting.Application.Queries;

public class GetReportQuery : IRequest<ApiResponse<ReportDto>>
{
    public Guid Id { get; set; }
}

public class GetReportsQuery : IRequest<ApiResponse<PagedResult<ReportSummaryDto>>>
{
    public ReportType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ReportDto
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

public class ReportSummaryDto
{
    public Guid Id { get; set; }
    public ReportType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}

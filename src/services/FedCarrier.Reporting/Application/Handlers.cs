using System.Text.Json;
using FedCarrier.Contracts;
using FedCarrier.Reporting.Application.Commands;
using FedCarrier.Reporting.Application.Queries;
using FedCarrier.Reporting.Domain;
using FedCarrier.Reporting.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Reporting.Application.Handlers;

public class GenerateReportCommandHandler : IRequestHandler<GenerateReportCommand, ApiResponse<Guid>>
{
    private readonly ReportingDbContext _db;
    public GenerateReportCommandHandler(ReportingDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(GenerateReportCommand request, CancellationToken ct)
    {
        var payload = BuildReportData(request.Type);
        var report = new Report
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Name = request.Name,
            Filters = request.Filters,
            Data = JsonSerializer.Serialize(payload),
            RowCount = payload.Count,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = request.GeneratedBy
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = report.Id };
    }

    private static List<Dictionary<string, object>> BuildReportData(ReportType type)
    {
        var rows = new List<Dictionary<string, object>>();
        switch (type)
        {
            case ReportType.Shipments:
                rows.Add(new Dictionary<string, object>
                {
                    ["summary"] = "Shipment volume",
                    ["value"] = 128
                });
                break;
            case ReportType.Financial:
                rows.Add(new Dictionary<string, object>
                {
                    ["summary"] = "Total revenue",
                    ["value"] = 25400.50
                });
                break;
            case ReportType.DriverPerformance:
                rows.Add(new Dictionary<string, object>
                {
                    ["summary"] = "Deliveries completed",
                    ["value"] = 96
                });
                break;
            default:
                rows.Add(new Dictionary<string, object> { ["summary"] = "Custom report", ["value"] = 0 });
                break;
        }
        return rows;
    }
}

public class CreateReportDefinitionCommandHandler : IRequestHandler<CreateReportDefinitionCommand, ApiResponse<Guid>>
{
    private readonly ReportingDbContext _db;
    public CreateReportDefinitionCommandHandler(ReportingDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(CreateReportDefinitionCommand request, CancellationToken ct)
    {
        var definition = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Name = request.Name,
            QueryTemplate = request.QueryTemplate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.ReportDefinitions.Add(definition);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = definition.Id };
    }
}

public class GetReportQueryHandler : IRequestHandler<GetReportQuery, ApiResponse<ReportDto>>
{
    private readonly ReportingDbContext _db;
    public GetReportQueryHandler(ReportingDbContext db) => _db = db;

    public async Task<ApiResponse<ReportDto>> Handle(GetReportQuery request, CancellationToken ct)
    {
        var report = await _db.Reports.FindAsync([request.Id], ct);
        if (report is null)
            return new ApiResponse<ReportDto> { Success = false, Errors = new List<string> { "Report not found" } };

        return new ApiResponse<ReportDto>
        {
            Success = true,
            Data = new ReportDto
            {
                Id = report.Id,
                Type = report.Type,
                Name = report.Name,
                Filters = report.Filters,
                Data = report.Data,
                RowCount = report.RowCount,
                GeneratedAt = report.GeneratedAt,
                GeneratedBy = report.GeneratedBy
            }
        };
    }
}

public class GetReportsQueryHandler : IRequestHandler<GetReportsQuery, ApiResponse<PagedResult<ReportSummaryDto>>>
{
    private readonly ReportingDbContext _db;
    public GetReportsQueryHandler(ReportingDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<ReportSummaryDto>>> Handle(GetReportsQuery request, CancellationToken ct)
    {
        var query = _db.Reports.AsQueryable();

        if (request.Type.HasValue)
            query = query.Where(r => r.Type == request.Type.Value);
        if (request.FromDate.HasValue)
            query = query.Where(r => r.GeneratedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(r => r.GeneratedAt <= request.ToDate.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.GeneratedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReportSummaryDto
            {
                Id = r.Id,
                Type = r.Type,
                Name = r.Name,
                RowCount = r.RowCount,
                GeneratedAt = r.GeneratedAt
            })
            .ToListAsync(ct);

        return new ApiResponse<PagedResult<ReportSummaryDto>>
        {
            Success = true,
            Data = new PagedResult<ReportSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}

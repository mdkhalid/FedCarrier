using FedCarrier.Contracts;
using FedCarrier.Reporting.Application.Commands;
using FedCarrier.Reporting.Application.Handlers;
using FedCarrier.Reporting.Application.Queries;
using FedCarrier.Reporting.Domain;
using FedCarrier.Reporting.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class ReportingServiceTests
{
    private ReportingDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task GenerateReportCommandHandler_ShouldCreateReport()
    {
        var db = GetDbContext();
        var handler = new GenerateReportCommandHandler(db);
        var command = new GenerateReportCommand
        {
            Type = ReportType.Shipments,
            Name = "Weekly Shipments",
            Filters = "week=current",
            GeneratedBy = Guid.NewGuid()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var report = await db.Reports.FindAsync(result.Data);
        report.RowCount.Should().BeGreaterThan(0);
        report.Data.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetReportQueryHandler_ShouldReturnReport()
    {
        var db = GetDbContext();
        var generateHandler = new GenerateReportCommandHandler(db);
        var reportId = (await generateHandler.Handle(new GenerateReportCommand
        {
            Type = ReportType.Financial,
            Name = "Monthly Revenue"
        }, CancellationToken.None)).Data;

        var handler = new GetReportQueryHandler(db);
        var query = new GetReportQuery { Id = reportId };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Name.Should().Be("Monthly Revenue");
        result.Data.Type.Should().Be(ReportType.Financial);
    }

    [Fact]
    public async Task GetReportsQueryHandler_ShouldFilterByType()
    {
        var db = GetDbContext();
        var generateHandler = new GenerateReportCommandHandler(db);
        await generateHandler.Handle(new GenerateReportCommand { Type = ReportType.Shipments, Name = "S1" }, CancellationToken.None);
        await generateHandler.Handle(new GenerateReportCommand { Type = ReportType.Financial, Name = "F1" }, CancellationToken.None);

        var handler = new GetReportsQueryHandler(db);
        var query = new GetReportsQuery { Type = ReportType.Shipments, Page = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items[0].Name.Should().Be("S1");
    }

    [Fact]
    public async Task CreateReportDefinitionCommandHandler_ShouldCreateDefinition()
    {
        var db = GetDbContext();
        var handler = new CreateReportDefinitionCommandHandler(db);
        var command = new CreateReportDefinitionCommand
        {
            Type = ReportType.Custom,
            Name = "CustomQuery",
            QueryTemplate = "SELECT * FROM shipments WHERE status = @status"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }
}

using Microsoft.EntityFrameworkCore;
using FedCarrier.Reporting.Domain;

namespace FedCarrier.Reporting.Infrastructure;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Type);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<ReportDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
        });
    }
}

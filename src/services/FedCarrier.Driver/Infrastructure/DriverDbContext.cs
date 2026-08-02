using Microsoft.EntityFrameworkCore;
using FedCarrier.Driver.Domain;

namespace FedCarrier.Driver.Infrastructure;

public class DriverDbContext : DbContext
{
    public DriverDbContext(DbContextOptions<DriverDbContext> options) : base(options) { }

    public DbSet<FedCarrier.Driver.Domain.Driver> Drivers => Set<FedCarrier.Driver.Domain.Driver>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FedCarrier.Driver.Domain.Driver>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseNumber).IsUnique();
            entity.Property(e => e.LicenseNumber).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.CurrentLocation).HasMaxLength(500);
        });
    }
}

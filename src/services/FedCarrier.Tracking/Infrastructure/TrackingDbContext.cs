using Microsoft.EntityFrameworkCore;
using FedCarrier.Tracking.Domain;

namespace FedCarrier.Tracking.Infrastructure;

public class TrackingDbContext : DbContext
{
    public TrackingDbContext(DbContextOptions<TrackingDbContext> options) : base(options) { }

    public DbSet<TrackingLocation> TrackingLocations => Set<TrackingLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackingLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShipmentId);
            entity.Property(e => e.Address).HasMaxLength(500);
        });
    }
}



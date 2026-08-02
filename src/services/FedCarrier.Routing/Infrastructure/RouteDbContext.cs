using Microsoft.EntityFrameworkCore;
using FedCarrier.Routing.Domain;

namespace FedCarrier.Routing.Infrastructure;

public class RouteDbContext : DbContext
{
    public RouteDbContext(DbContextOptions<RouteDbContext> options) : base(options) { }

    public DbSet<RoutePlan> RoutePlans => Set<RoutePlan>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoutePlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DriverId);
            entity.Property(e => e.OriginAddress).HasMaxLength(500);
            entity.Property(e => e.DestinationAddress).HasMaxLength(500);
        });

        modelBuilder.Entity<RouteStop>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RoutePlanId);
            entity.Property(e => e.Address).HasMaxLength(500);
        });
    }
}

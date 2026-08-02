using Microsoft.EntityFrameworkCore;
using FedCarrier.Shipment.Domain;

namespace FedCarrier.Shipment.Infrastructure;

public class ShipmentDbContext : DbContext
{
    public ShipmentDbContext(DbContextOptions<ShipmentDbContext> options) : base(options) { }

    public DbSet<FedCarrier.Shipment.Domain.Shipment> Shipments => Set<FedCarrier.Shipment.Domain.Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<StatusHistory> StatusHistory => Set<StatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FedCarrier.Shipment.Domain.Shipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Origin).HasMaxLength(200);
            entity.Property(e => e.Destination).HasMaxLength(200);
        });

        modelBuilder.Entity<ShipmentItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShipmentId);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<StatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShipmentId);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ChangedBy).HasMaxLength(100);
        });
    }
}

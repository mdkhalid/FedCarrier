using Microsoft.EntityFrameworkCore;
using FedCarrier.Warehouse.Domain;

namespace FedCarrier.Warehouse.Infrastructure;

public class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

    public DbSet<FedCarrier.Warehouse.Domain.Warehouse> Warehouses => Set<FedCarrier.Warehouse.Domain.Warehouse>();
    public DbSet<Inventory> Inventories => Set<Inventory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FedCarrier.Warehouse.Domain.Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(20);
            entity.Property(e => e.Country).HasMaxLength(100);
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WarehouseId);
            entity.Property(e => e.ProductName).HasMaxLength(500);
        });
    }
}

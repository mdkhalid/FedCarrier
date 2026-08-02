using Microsoft.EntityFrameworkCore;
using FedCarrier.Order.Domain;

namespace FedCarrier.Order.Infrastructure;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<FedCarrier.Order.Domain.Order> Orders => Set<FedCarrier.Order.Domain.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FedCarrier.Order.Domain.Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.Property(e => e.ProductName).HasMaxLength(500);
        });
    }
}

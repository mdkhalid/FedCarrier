using Microsoft.EntityFrameworkCore;
using FedCarrier.Customer.Domain;

namespace FedCarrier.Customer.Infrastructure;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options) { }

    public DbSet<FedCarrier.Customer.Domain.Customer> Customers => Set<FedCarrier.Customer.Domain.Customer>();
    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FedCarrier.Customer.Domain.Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CustomerId, e.IsDefault });
            entity.Property(e => e.Street).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(20);
            entity.Property(e => e.Country).HasMaxLength(100);
        });
    }
}

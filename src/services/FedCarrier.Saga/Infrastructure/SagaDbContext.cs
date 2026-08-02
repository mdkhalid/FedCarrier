using FedCarrier.Saga.Domain;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Saga.Infrastructure;

public class SagaDbContext : DbContext
{
    public SagaDbContext(DbContextOptions<SagaDbContext> options) : base(options) { }

    public DbSet<SagaState> SagaStates => Set<SagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SagaState>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => new { e.Status, e.CurrentStep });
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
            entity.Property(e => e.FailureReason).HasMaxLength(1000);
        });
    }
}

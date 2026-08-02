using Microsoft.EntityFrameworkCore;
using FedCarrier.Notification.Domain;

namespace FedCarrier.Notification.Infrastructure;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Domain.Notification> Notifications => Set<Domain.Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Type });
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.Recipient).HasMaxLength(256);
            entity.Property(e => e.Error).HasMaxLength(1000);
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
        });
    }
}

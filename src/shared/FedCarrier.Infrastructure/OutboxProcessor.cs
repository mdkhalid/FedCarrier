using System.Text.Json;
using FedCarrier.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FedCarrier.Infrastructure;

public class OutboxOptions
{
    public int BatchSize { get; set; } = 50;
    public int PollIntervalSeconds { get; set; } = 5;
    public bool Enabled { get; set; } = true;
}

public class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Processed, e.OccurredOn });
            entity.Property(e => e.AggregateId).HasMaxLength(100);
            entity.Property(e => e.EventType).HasMaxLength(200);
            entity.Property(e => e.Content).HasMaxLength(8000);
        });
    }
}

public class EfOutboxRepository : IOutboxRepository
{
    private readonly OutboxDbContext _db;
    public EfOutboxRepository(OutboxDbContext db) => _db = db;

    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _db.OutboxMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _db.OutboxMessages
            .Where(m => !m.Processed)
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _db.OutboxMessages.FindAsync([id], cancellationToken);
        if (message is not null)
        {
            message.Processed = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

public class OutboxPublisher
{
    private readonly IOutboxRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(IOutboxRepository repository, IEventBus eventBus, ILogger<OutboxPublisher> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task AddAsync<T>(T @event, string aggregateId, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            EventType = typeof(T).Name,
            Content = JsonSerializer.Serialize(@event),
            OccurredOn = @event.OccurredOn,
            Processed = false
        };
        await _repository.SaveAsync(message, cancellationToken);
    }

    public async Task ProcessPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var pending = await _repository.GetPendingAsync(batchSize, cancellationToken);
        foreach (var message in pending)
        {
            try
            {
                var eventType = Type.GetType("FedCarrier.Contracts." + message.EventType + ", FedCarrier.Contracts");
                if (eventType is null)
                {
                    _logger.LogWarning("Unknown event type {Type}", message.EventType);
                    await _repository.MarkAsProcessedAsync(message.Id, cancellationToken);
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Content, eventType) as IntegrationEvent;
                if (@event is null)
                {
                    await _repository.MarkAsProcessedAsync(message.Id, cancellationToken);
                    continue;
                }

                await _eventBus.PublishAsync(@event, message.EventType.ToLowerInvariant().Replace("event", "", StringComparison.Ordinal), cancellationToken);
                await _repository.MarkAsProcessedAsync(message.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {Id} ({Type})", message.Id, message.EventType);
            }
        }
    }
}

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceProvider services, IOptions<OutboxOptions> options, ILogger<OutboxProcessor> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Outbox processor disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<OutboxPublisher>();
                await publisher.ProcessPendingAsync(_options.BatchSize, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }
}

using System.Text.Json;
using FedCarrier.Contracts;

namespace FedCarrier.Infrastructure;

public static class OutboxWriter
{
    public static async Task WriteAsync<T>(
        IOutboxRepository? repository,
        T @event,
        string aggregateId,
        CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        if (repository is null)
            return;

        await repository.SaveAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            EventType = typeof(T).Name,
            Content = JsonSerializer.Serialize(@event),
            OccurredOn = @event.OccurredOn,
            Processed = false
        }, cancellationToken);
    }
}

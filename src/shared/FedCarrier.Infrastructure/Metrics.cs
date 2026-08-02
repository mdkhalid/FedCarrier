using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FedCarrier.Infrastructure;

public static class FedCarrierMetrics
{
    private static readonly Meter Meter = new("FedCarrier.Messaging", "1.0.0");

    public static readonly Counter<long> MessagesPublished = Meter.CreateCounter<long>(
        "fedcarrier.messaging.published", "events", "Events published to RabbitMQ");

    public static readonly Counter<long> MessagesReceived = Meter.CreateCounter<long>(
        "fedcarrier.messaging.received", "events", "Events received from RabbitMQ");

    public static readonly Counter<long> MessagesFailed = Meter.CreateCounter<long>(
        "fedcarrier.messaging.failed", "events", "Events that failed processing");

    public static readonly Counter<long> MessagesDeadLettered = Meter.CreateCounter<long>(
        "fedcarrier.messaging.deadlettered", "events", "Events moved to DLQ");

    public static readonly Counter<long> MessagesRetried = Meter.CreateCounter<long>(
        "fedcarrier.messaging.retried", "events", "Events scheduled for retry");

    public static readonly Counter<long> OutboxProcessed = Meter.CreateCounter<long>(
        "fedcarrier.outbox.processed", "messages", "Outbox messages processed");

    public static readonly Counter<long> OutboxPublished = Meter.CreateCounter<long>(
        "fedcarrier.outbox.published", "messages", "Outbox messages published");

    public static ActivitySource ActivitySource { get; } = new("FedCarrier.Messaging");
}

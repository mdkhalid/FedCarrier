using FedCarrier.Contracts;

namespace FedCarrier.Infrastructure;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;
    Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, string? queue = null, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public class EventBusOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "fedcarrier";
    public string Password { get; set; } = "fedcarrier@dev123!";
    public string Exchange { get; set; } = "fedcarrier.events";
    public string DlqExchange { get; set; } = "fedcarrier.dlx";
    public string ServiceName { get; set; } = "FedCarrier";
    public bool Enabled { get; set; } = true;
}

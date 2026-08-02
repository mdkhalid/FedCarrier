using System.Text;
using System.Text.Json;
using FedCarrier.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FedCarrier.Infrastructure;

public class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly EventBusOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly Dictionary<string, List<Func<object, CancellationToken, Task>>> _handlers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly List<string> _consumerTags = new();

    public RabbitMqEventBus(IOptions<EventBusOptions> options, ILogger<RabbitMqEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Event bus is disabled");
            return;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null)
                return;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                AutomaticRecoveryEnabled = true
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
            await _channel.ExchangeDeclareAsync(_options.DlqExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", _options.HostName, _options.Port);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        await PublishAsync(@event, GetDefaultRoutingKey<T>(), cancellationToken);
    }

    public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Event bus disabled; event {Type} not published", typeof(T).Name);
            return;
        }

        await StartAsync(cancellationToken);
        if (_channel is null)
        {
            _logger.LogWarning("Cannot publish {Type}: channel unavailable", typeof(T).Name);
            return;
        }

        @event.EventType = typeof(T).Name;
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, @event.GetType()));
        var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await _channel.BasicPublishAsync(
            _options.Exchange,
            routingKey,
            mandatory: false,
            properties,
            body,
            cancellationToken);

        _logger.LogInformation("Published {Type} to {RoutingKey}", typeof(T).Name, routingKey);
    }

    public async Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, string? queue = null, CancellationToken cancellationToken = default)
        where T : IntegrationEvent
    {
        if (!_options.Enabled)
            return;

        await StartAsync(cancellationToken);
        if (_channel is null)
            return;

        var routingKey = GetDefaultRoutingKey<T>();
        var queueName = queue ?? _options.ServiceName + "." + routingKey + "." + typeof(T).Name;
        var dlqName = queueName + ".dlq";

        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = _options.DlqExchange,
            ["x-dead-letter-routing-key"] = routingKey + ".dlq"
        }, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(queueName, _options.Exchange, routingKey, cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(dlqName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(dlqName, _options.DlqExchange, routingKey + ".dlq", cancellationToken: cancellationToken);

        var eventName = typeof(T).Name;
        lock (_handlers)
        {
            if (!_handlers.ContainsKey(eventName))
                _handlers[eventName] = new List<Func<object, CancellationToken, Task>>();
            _handlers[eventName].Add(async (message, ct) => await handler((T)message, ct));
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                var @event = JsonSerializer.Deserialize<T>(message);
                if (@event is not null)
                {
                    List<Func<object, CancellationToken, Task>> handlers;
                    lock (_handlers)
                    {
                        handlers = _handlers.TryGetValue(eventName, out var h) ? new List<Func<object, CancellationToken, Task>>(h) : new();
                    }
                    foreach (var h in handlers)
                        await h(@event, CancellationToken.None);
                }
                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event {Type}", typeof(T).Name);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken);
            }
        };

        var tag = await _channel.BasicConsumeAsync(queueName, autoAck: false, _options.ServiceName + "-" + Guid.NewGuid().ToString("N"), false, false, null, consumer, cancellationToken);
        _consumerTags.Add(tag);
        _logger.LogInformation("Subscribed to {Queue} for {Type}", queueName, typeof(T).Name);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_channel is not null)
        {
            foreach (var tag in _consumerTags)
            {
                try { await _channel.BasicCancelAsync(tag, false, cancellationToken); } catch { /* ignore */ }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _channel?.Dispose();
        _connection?.Dispose();
        _semaphore.Dispose();
    }

    private static string GetDefaultRoutingKey<T>()
    {
        return typeof(T).Name.ToLowerInvariant().Replace("event", "", StringComparison.Ordinal);
    }
}

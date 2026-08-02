using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FedCarrier.Infrastructure;

public static class MessagingServiceExtensions
{
    public static IServiceCollection AddOutbox(this IServiceCollection services, string? connectionString = null)
    {
        services.AddDbContext<OutboxDbContext>(options =>
        {
            if (connectionString is not null)
                options.UseSqlServer(connectionString);
        });

        services.AddScoped<IOutboxRepository, EfOutboxRepository>();
        services.AddScoped<OutboxPublisher>();
        services.AddHostedService<OutboxProcessor>();
        return services;
    }

    public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EventBusOptions>(configuration.GetSection("RabbitMQ"));
        services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        return services;
    }

    public static IServiceCollection AddEventBus(this IServiceCollection services, Action<EventBusOptions> configure)
    {
        services.Configure(configure);
        services.Configure<OutboxOptions>(o => { });
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        return services;
    }
}

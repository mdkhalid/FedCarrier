using Serilog;
using FedCarrier.Notification.Application.Commands;
using FedCarrier.Notification.Domain;
using FedCarrier.Notification.Infrastructure;
using FedCarrier.Contracts;
using FedCarrier.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("ServiceName", context.HostingEnvironment.ApplicationName);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEventBus(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var eventBus = app.Services.GetRequiredService<IEventBus>();
var serviceProvider = app.Services;

app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await eventBus.SubscribeAsync<NotifyCustomerCommandEvent>(async (@event, ct) =>
        {
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            await mediator.Send(new SendNotificationCommand
            {
                UserId = @event.CustomerId,
                Type = NotificationType.Payment,
                Channel = @event.Channel == "Sms" ? NotificationChannel.Sms : NotificationChannel.Email,
                Subject = @event.Title,
                Body = @event.Message,
                Recipient = @event.CustomerName
            }, ct);
        }, "FedCarrier.Notification.saga");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to subscribe notification event handlers");
    }
});

app.Run();

using Serilog;
using Microsoft.EntityFrameworkCore;
using FedCarrier.Shipment.Application.Commands;
using FedCarrier.Shipment.Infrastructure;
using FedCarrier.Contracts;
using FedCarrier.Infrastructure;
using MediatR;

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
builder.Services.AddDbContext<ShipmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEventBus(builder.Configuration);
builder.Services.AddOutbox(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddObservability(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseObservability();

var eventBus = app.Services.GetRequiredService<IEventBus>();
var serviceProvider = app.Services;

app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await eventBus.SubscribeAsync<CreateShipmentCommandEvent>(async (@event, ct) =>
        {
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            await mediator.Send(new CreateShipmentCommand
            {
                OrderId = @event.OrderId,
                Origin = string.IsNullOrEmpty(@event.Origin) ? "Origin" : @event.Origin,
                Destination = string.IsNullOrEmpty(@event.Destination) ? "Destination" : @event.Destination,
                CorrelationId = @event.CorrelationId
            }, ct);
        }, "FedCarrier.Shipment.saga");

        await eventBus.SubscribeAsync<CancelShipmentCommandEvent>(async (@event, ct) =>
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ShipmentDbContext>();
            var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.OrderId == @event.OrderId, ct);
            if (shipment is null)
                return;
            shipment.Status = FedCarrier.Shipment.Domain.ShipmentStatus.Cancelled;
            shipment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }, "FedCarrier.Shipment.saga");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to subscribe shipment event handlers");
    }
});

app.Run();

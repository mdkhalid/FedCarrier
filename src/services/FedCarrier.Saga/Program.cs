using Serilog;
using FedCarrier.Contracts;
using FedCarrier.Infrastructure;
using FedCarrier.Saga.Application;
using FedCarrier.Saga.Infrastructure;
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
builder.Services.AddDbContext<SagaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<SagaOrchestrator>();
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
app.MapHealthChecks("/health");

var orchestrator = app.Services.GetRequiredService<SagaOrchestrator>();
var eventBus = app.Services.GetRequiredService<IEventBus>();

app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await eventBus.SubscribeAsync<OrderPlacedEvent>(orchestrator.HandleOrderPlacedAsync, "FedCarrier.Saga.orders");
        await eventBus.SubscribeAsync<ShipmentCreatedEvent>(orchestrator.HandleShipmentCreatedAsync, "FedCarrier.Saga.shipments");
        await eventBus.SubscribeAsync<ShipmentDeliveredEvent>(orchestrator.HandleShipmentDeliveredAsync, "FedCarrier.Saga.shipments");
        await eventBus.SubscribeAsync<InvoiceGeneratedEvent>(orchestrator.HandleInvoiceGeneratedAsync, "FedCarrier.Saga.billing");
        await eventBus.SubscribeAsync<PaymentConfirmedEvent>(orchestrator.HandlePaymentConfirmedAsync, "FedCarrier.Saga.billing");
        await eventBus.SubscribeAsync<OrderStatusChangedEvent>(orchestrator.HandleOrderCancelledAsync, "FedCarrier.Saga.orders");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to subscribe saga event handlers");
    }
});

app.Run();

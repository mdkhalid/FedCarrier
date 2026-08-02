using Serilog;
using FedCarrier.Billing.Application.Commands;
using FedCarrier.Billing.Infrastructure;
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
builder.Services.AddDbContext<BillingDbContext>(options =>
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
        await eventBus.SubscribeAsync<CreateInvoiceCommandEvent>(async (@event, ct) =>
        {
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            await mediator.Send(new CreateInvoiceCommand
            {
                ShipmentId = @event.ShipmentId,
                CustomerId = @event.CustomerId,
                Amount = @event.Amount,
                TaxRate = 0.1m,
                DueDate = DateTime.UtcNow.AddDays(14),
                CorrelationId = @event.CorrelationId
            }, ct);
        }, "FedCarrier.Billing.saga");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to subscribe billing event handlers");
    }
});

app.Run();

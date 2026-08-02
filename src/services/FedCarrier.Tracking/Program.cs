using Serilog;
using FedCarrier.Tracking.Infrastructure;
using FedCarrier.Tracking.Application.Handlers;
using FedCarrier.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using FedCarrier.Tracking.Controllers;

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
builder.Services.AddDbContext<TrackingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSignalR();
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
app.MapHub<TrackingHub>("/hubs/tracking");
app.UseObservability();

app.Run();



using Serilog;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("ServiceName", "FedCarrier.Gateway");
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseRouting();
app.Use((context, next) =>
{
    context.Response.Headers.Add("X-Request-Id", context.TraceIdentifier);
    return next(context);
});
app.MapReverseProxy();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

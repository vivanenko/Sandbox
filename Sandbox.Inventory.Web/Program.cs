using MassTransit;
using MassTransit.RabbitMqTransport.Configuration;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sandbox.Inventory;
using Sandbox.Inventory.Shared;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Inventory"))
    .WithTracing(tracing =>
    {
        tracing
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName);
        tracing.AddOtlpExporter();
    });

builder.Services.AddOpenApi();

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddConsumer<ReserveInventoryConsumer>().Endpoint(c =>
    {
        c.Name = "inventory:reserve-inventory";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<ReleaseInventoryConsumer>().Endpoint(c =>
    {
        c.Name = "inventory:release-inventory";
        c.ConfigureConsumeTopology = false;
    });

    cfg.UsingRabbitMq((context, config) =>
    {
        config.Host("localhost", 5673, "/", _ => { });
        config.ConfigureEndpoints(context);
        
        config.Message<InventoryReserved>(x => x.SetEntityName("inventory:inventory-reserved"));
        config.Message<InventoryReservationFailed>(x => x.SetEntityName("inventory:inventory-reservation-failed"));
        config.Message<InventoryReleased>(x => x.SetEntityName("inventory:inventory-released"));
        config.Message<InventoryReleaseFailed>(x => x.SetEntityName("inventory:inventory-release-failed"));
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.Run();
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sandbox.Stock;
using Sandbox.Stock.Shared;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Stock"))
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
    cfg.AddConsumer<ReserveStockConsumer>().Endpoint(c =>
    {
        c.Name = "stock:reserve-stock";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<ReleaseStockConsumer>().Endpoint(c =>
    {
        c.Name = "stock:release-stock";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<ExtendStockReservationConsumer>().Endpoint(c =>
    {
        c.Name = "stock:extend-stock-reservation";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<ReduceStockReservationConsumer>().Endpoint(c =>
    {
        c.Name = "stock:reduce-stock-reservation";
        c.ConfigureConsumeTopology = false;
    });

    cfg.UsingRabbitMq((context, config) =>
    {
        config.Host("localhost", 5673, "/", _ => { });
        config.ConfigureEndpoints(context);
        
        config.Message<StockReserved>(x => x.SetEntityName("stock:stock-reserved"));
        config.Message<StockReservationFailed>(x => x.SetEntityName("stock:stock-reservation-failed"));
        
        config.Message<StockReleased>(x => x.SetEntityName("stock:stock-released"));
        config.Message<StockReleaseFailed>(x => x.SetEntityName("stock:stock-release-failed"));
        
        config.Message<StockReservationExtended>(x => x.SetEntityName("stock:stock-reservation-extended"));
        config.Message<StockReservationExtensionFailed>(x => x.SetEntityName("stock:stock-reservation-extension-failed"));

        config.Message<StockReservationReduced>(x => x.SetEntityName("stock:stock-reservation-reduced"));
        config.Message<StockReservationReductionFailed>(x => x.SetEntityName("stock:stock-reservation-reduction-failed"));
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.Run();
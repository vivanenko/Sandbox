using dotenv.net;
using Grafana.OpenTelemetry;
using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sandbox.Stock;
using Sandbox.Stock.Shared;

const string serviceName = "Stock";

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(configure =>
    {
        configure
            .UseGrafana(grafana =>
            {
                grafana.ServiceName = serviceName;
            })
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName);
    })
    .WithMetrics(configure =>
    {
        configure
            .UseGrafana(grafana =>
            {
                grafana.ServiceName = serviceName;
            })
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

builder.Logging.AddOpenTelemetry(options =>
{
    options.UseGrafana(grafana =>
    {
        grafana.ServiceName = serviceName;
    });
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
    cfg.AddConsumer<ConfirmStockReservationConsumer>().Endpoint(c =>
    {
        c.Name = "stock:confirm-stock-reservation";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<RevertStockReservationConsumer>().Endpoint(c =>
    {
        c.Name = "stock:revert-stock-reservation";
        c.ConfigureConsumeTopology = false;
    });

    cfg.UsingRabbitMq((context, config) =>
    {
        var rabbitMq = builder.Configuration.GetSection("RabbitMQ");
        config.Host(rabbitMq["Host"], ushort.Parse(rabbitMq["Port"]), rabbitMq["VirtualHost"], _ => { });
        config.ConfigureEndpoints(context);
        
        config.Message<StockReserved>(x => x.SetEntityName("stock:stock-reserved"));
        config.Message<StockReservationFailed>(x => x.SetEntityName("stock:stock-reservation-failed"));
        
        config.Message<StockReleased>(x => x.SetEntityName("stock:stock-released"));
        config.Message<StockReleaseFailed>(x => x.SetEntityName("stock:stock-release-failed"));
        
        config.Message<StockReservationExtended>(x => x.SetEntityName("stock:stock-reservation-extended"));
        config.Message<StockReservationExtensionFailed>(x => x.SetEntityName("stock:stock-reservation-extension-failed"));

        config.Message<StockReservationReduced>(x => x.SetEntityName("stock:stock-reservation-reduced"));
        config.Message<StockReservationReductionFailed>(x => x.SetEntityName("stock:stock-reservation-reduction-failed"));
        
        config.Message<StockReservationConfirmed>(x => x.SetEntityName("stock:stock-reservation-confirmed"));
        config.Message<StockReservationConfirmationFailed>(x => x.SetEntityName("stock:stock-reservation-confirmation-failed"));
        
        config.Message<StockReservationReverted>(x => x.SetEntityName("stock:stock-reservation-reverted"));
        config.Message<StockReservationReversionFailed>(x => x.SetEntityName("stock:stock-reservation-reversion-failed"));
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
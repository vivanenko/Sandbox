using dotenv.net;
using Grafana.OpenTelemetry;
using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sandbox.Wallet;
using Sandbox.Wallet.Shared;

const string serviceName = "Wallet";

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
    cfg.AddConsumer<HoldCoinsConsumer>().Endpoint(c =>
    {
        c.Name = "wallet:hold-coins";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<CancelHoldConsumer>().Endpoint(c =>
    {
        c.Name = "wallet:cancel-hold";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<CommitHoldConsumer>().Endpoint(c =>
    {
        c.Name = "wallet:commit-hold";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<RefundCoinsConsumer>().Endpoint(c =>
    {
        c.Name = "wallet:refund-coins";
        c.ConfigureConsumeTopology = false;
    });
    
    cfg.UsingRabbitMq((context, config) =>
    {
        var rabbitMq = builder.Configuration.GetSection("RabbitMQ");
        config.Host(rabbitMq["Host"], ushort.Parse(rabbitMq["Port"]), rabbitMq["VirtualHost"], _ => { });
        config.ConfigureEndpoints(context);
        
        config.Message<CoinsHeld>(x => x.SetEntityName("wallet:coins-held"));
        config.Message<CoinsHoldFailed>(x => x.SetEntityName("wallet:coins-hold-failed"));
        config.Message<HoldCancelled>(x => x.SetEntityName("wallet:hold-cancelled"));
        config.Message<HoldCancellationFailed>(x => x.SetEntityName("wallet:hold-cancellation-failed"));
        config.Message<HoldCommitted>(x => x.SetEntityName("wallet:hold-committed"));
        config.Message<HoldCommitFailed>(x => x.SetEntityName("wallet:hold-commit-failed"));
        config.Message<CoinsRefunded>(x => x.SetEntityName("wallet:coins-refunded"));
        config.Message<CoinsRefundFailed>(x => x.SetEntityName("wallet:coins-refund-failed"));
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
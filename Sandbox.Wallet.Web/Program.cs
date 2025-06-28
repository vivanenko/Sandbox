using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sandbox.Wallet;
using Sandbox.Wallet.Shared;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Wallet"))
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
        config.Host("localhost", 5673, "/", _ => { });
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

app.UseSerilogRequestLogging();

app.Run();
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sandbox.Wallet;
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
    cfg.AddConsumer<HoldCoinsConsumer>().Endpoint(c => c.Name = "wallet:hold-coins");
    cfg.AddConsumer<CancelHoldConsumer>().Endpoint(c => c.Name = "wallet:cancel-hold");
    cfg.AddConsumer<CommitHoldConsumer>().Endpoint(c => c.Name = "wallet:commit-hold");
    cfg.AddConsumer<RefundCoinsConsumer>().Endpoint(c => c.Name = "wallet:refund-coins");
    
    cfg.UsingRabbitMq((context, config) =>
    {
        config.Host("localhost", 5673, "/", _ => { });
        config.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.Run();
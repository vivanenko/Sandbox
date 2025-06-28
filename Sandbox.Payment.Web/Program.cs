using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sandbox.Payment;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Payment"))
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
    cfg.AddConsumer<CreatePaymentIntentConsumer>().Endpoint(c => c.Name = "create-payment-intent");
    cfg.AddConsumer<CancelPaymentIntentConsumer>().Endpoint(c => c.Name = "cancel-payment-intent");
    cfg.AddConsumer<ConfirmPaymentConsumer>().Endpoint(c => c.Name = "confirm-payment");
    cfg.AddConsumer<RefundPaymentConsumer>().Endpoint(c => c.Name = "refund-payment");
    
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
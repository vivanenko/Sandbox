using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sandbox.Payment;
using Sandbox.Payment.Shared;
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
    cfg.AddConsumer<CreatePaymentIntentConsumer>().Endpoint(c =>
    {
        c.Name = "payment:create-payment-intent";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<CancelPaymentIntentConsumer>().Endpoint(c =>
    {
        c.Name = "payment:cancel-payment-intent";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<ConfirmPaymentConsumer>().Endpoint(c =>
    {
        c.Name = "payment:confirm-payment";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<RefundPaymentConsumer>().Endpoint(c =>
    {
        c.Name = "payment:refund-payment";
        c.ConfigureConsumeTopology = false;
    });
    
    cfg.UsingRabbitMq((context, config) =>
    {
        var rabbitMq = builder.Configuration.GetSection("RabbitMQ");
        config.Host(rabbitMq["Host"], ushort.Parse(rabbitMq["Port"]), rabbitMq["VirtualHost"], _ => { });
        config.ConfigureEndpoints(context);
        
        config.Message<PaymentIntentCreated>(x => x.SetEntityName("payment:payment-intent-created"));
        config.Message<PaymentIntentFailed>(x => x.SetEntityName("payment:payment-intent-failed"));
        config.Message<PaymentIntentCancelled>(x => x.SetEntityName("payment:payment-intent-cancelled"));
        config.Message<PaymentIntentCancellationFailed>(x => x.SetEntityName("payment:payment-intent-cancellation-failed"));
        config.Message<PaymentConfirmed>(x => x.SetEntityName("payment:payment-confirmed"));
        config.Message<PaymentFailed>(x => x.SetEntityName("payment:payment-failed"));
        config.Message<PaymentRefunded>(x => x.SetEntityName("payment:payment-refunded"));
        config.Message<PaymentRefundFailed>(x => x.SetEntityName("payment:payment-refund-failed"));
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.Run();
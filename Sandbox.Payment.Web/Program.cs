using MassTransit;
using Sandbox.Payment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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

app.Run();
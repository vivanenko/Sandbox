using MassTransit;
using Sandbox.Wallet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddConsumer<HoldCoinsConsumer>().Endpoint(c => c.Name = "hold-coins");
    cfg.AddConsumer<CancelHoldConsumer>().Endpoint(c => c.Name = "cancel-hold");
    cfg.AddConsumer<CommitHoldConsumer>().Endpoint(c => c.Name = "commit-hold");
    cfg.AddConsumer<RefundCoinsConsumer>().Endpoint(c => c.Name = "refund-coins");
    
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

app.Run();
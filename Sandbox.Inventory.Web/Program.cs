using MassTransit;
using Sandbox.Inventory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddConsumer<ReserveInventoryConsumer>().Endpoint(c => c.Name = "reserve-inventory");
    cfg.AddConsumer<ReleaseInventoryConsumer>().Endpoint(c => c.Name = "release-inventory");

    cfg.UsingRabbitMq((context, config) =>
    {
        config.Host("localhost", 5673, "/", _ => { });
        config.ConfigureEndpoints(context);
    });
});

app.Run();
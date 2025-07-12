using dotenv.net;
using Grafana.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sandbox.Cart;

const string serviceName = "Cart";

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
            .AddHttpClientInstrumentation();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/cart", async () =>
{
    var cart = new Cart { CartItems = [new CartItem { Id = Guid.CreateVersion7() }] };
    return Results.Ok(cart);
});

app.Run();
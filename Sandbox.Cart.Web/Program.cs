using dotenv.net;
using Grafana.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

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
    return Results.Ok(new { Items = new[] { new { Id = 1, Name = "Item 1" }, new { Id = 2, Name = "Item 2" } } });
});

app.Run();
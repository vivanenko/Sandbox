using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sandbox.Ordering;
using Sandbox.Ordering.Sagas.OrderPayment;
using Sandbox.Ordering.Sagas.OrderPlacement;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Ordering"))
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
    cfg.AddSagaStateMachine<OrderPlacementStateMachine, OrderPlacementState>()
        .Endpoint(e => e.Name = "ordering:order-placement-saga-state")
        .InMemoryRepository();
    cfg.AddSagaStateMachine<OrderPaymentStateMachine, OrderPaymentState>()
        .Endpoint(e => e.Name = "ordering:order-payment-saga-state")
        .InMemoryRepository();
    
    cfg.AddRequestClient<StartOrderPlacementSaga>();
    cfg.AddRequestClient<StartOrderPaymentSaga>();
    
    cfg.AddConsumer<PlaceOrderConsumer>().Endpoint(c => c.Name = "ordering:place-order");
    cfg.AddConsumer<MoveOrderToPaidStateConsumer>().Endpoint(c => c.Name = "ordering:move-order-to-paid-state");
    
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

app.MapGet("checkout/run", async (IRequestClient<StartOrderPlacementSaga> requestClient,
    CancellationToken cancellationToken) =>
{
    var command = new StartOrderPlacementSaga(Guid.CreateVersion7(), Guid.CreateVersion7(), 10, 5, []);
    var response = await requestClient
        .GetResponse<OrderPlacementSagaCompleted, OrderPlacementSagaFailed>(command, cancellationToken);

    if (response.Is(out Response<OrderPlacementSagaCompleted>? completed))
    {
        if (completed.Message.IsPaymentConfirmationRequired)
        {
            Console.WriteLine("Checkout: Confirmation required");
            return Results.Ok(completed.Message.OrderId);
        }

        Console.WriteLine("Checkout completed");
        return Results.Ok("Checkout completed");
    }

    if (response.Is(out Response<OrderPlacementSagaFailed>? failed))
    {
        Console.WriteLine("Checkout failed");
        return Results.BadRequest(failed.Message.Reason);
    }
    
    throw new Exception("Unknown response");
});

app.MapGet("checkout/{orderId}/confirm", async (Guid orderId, IRequestClient<StartOrderPaymentSaga> requestClient,
    CancellationToken cancellationToken) =>
{
    var command = new StartOrderPaymentSaga(orderId, Guid.CreateVersion7());
    var response = await requestClient
        .GetResponse<OrderPaymentSagaCompleted, OrderPaymentSagaFailed>(command, cancellationToken);

    if (response.Is(out Response<OrderPaymentSagaCompleted>? succeeded))
    {
        Console.WriteLine("Checkout completed");
        return Results.Ok("Checkout completed");
    }

    if (response.Is(out Response<OrderPaymentSagaFailed>? failed))
    {
        Console.WriteLine("Checkout failed");
        return Results.BadRequest(failed.Message.Reason);
    }
    
    throw new Exception("Unknown response");
});

app.Run();
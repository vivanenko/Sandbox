using MassTransit;
using MongoDB.Bson.Serialization;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sandbox.Inventory.Shared;
using Sandbox.Ordering;
using Sandbox.Ordering.Sagas.OrderPayment;
using Sandbox.Ordering.Sagas.OrderPayment.MongoDb;
using Sandbox.Ordering.Sagas.OrderPlacement;
using Sandbox.Ordering.Sagas.OrderPlacement.MongoDb;
using Sandbox.Ordering.Shared;
using Sandbox.Payment.Shared;
using Sandbox.Wallet.Shared;
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

builder.Services.AddSingleton<BsonClassMap<OrderPlacementState>, OrderPlacementStateClassMap>();
builder.Services.AddSingleton<BsonClassMap<OrderPaymentState>, OrderPaymentStateClassMap>();
builder.Services.AddMassTransit(cfg =>
{
    cfg.AddSagaStateMachine<OrderPlacementStateMachine, OrderPlacementState>()
        .MongoDbRepository(r =>
        {
            r.Connection = "mongodb://localhost:27017";
            r.DatabaseName = "orderPlacementSaga";
            r.CollectionName = "orderPlacementSagaStates";
        })
        .Endpoint(e => e.Name = "ordering:order-placement-saga-state");
    cfg.AddSagaStateMachine<OrderPaymentStateMachine, OrderPaymentState>()
        .MongoDbRepository(r =>
        {
            r.Connection = "mongodb://localhost:27017";
            r.DatabaseName = "orderPaymentSaga";
            r.CollectionName = "orderPaymentSagaStates";
        })
        .Endpoint(e => e.Name = "ordering:order-payment-saga-state");
    
    cfg.AddRequestClient<StartOrderPlacementSaga>();
    cfg.AddRequestClient<StartOrderPaymentSaga>();
    
    cfg.AddConsumer<PlaceOrderConsumer>().Endpoint(c =>
    {
        c.Name = "ordering:place-order";
        c.ConfigureConsumeTopology = false;
    });
    cfg.AddConsumer<MoveOrderToPaidStateConsumer>().Endpoint(c =>
    {
        c.Name = "ordering:move-order-to-paid-state";
        c.ConfigureConsumeTopology = false;
    });
    
    cfg.UsingRabbitMq((context, config) =>
    {
        config.Host("localhost", 5673, "/", _ => { });
        config.ConfigureEndpoints(context);
        
        config.Message<StartOrderPlacementSaga>(x => x.SetEntityName("ordering:start-order-placement-saga"));
        config.Message<OrderPlacementSagaCompleted>(x => x.SetEntityName("ordering:order-placement-saga-completed"));
        config.Message<OrderPlacementSagaFailed>(x => x.SetEntityName("ordering:order-placement-saga-failed"));
        
        config.Message<StartOrderPaymentSaga>(x => x.SetEntityName("ordering:start-order-payment-saga"));
        config.Message<OrderPaymentSagaCompleted>(x => x.SetEntityName("ordering:order-payment-saga-completed"));
        config.Message<OrderPaymentSagaFailed>(x => x.SetEntityName("ordering:order-payment-saga-failed"));
        
        config.Message<InventoryReserved>(x => x.SetEntityName("inventory:inventory-reserved"));
        config.Message<InventoryReservationFailed>(x => x.SetEntityName("inventory:inventory-reservation-failed"));
        config.Message<InventoryReleased>(x => x.SetEntityName("inventory:inventory-released"));
        config.Message<InventoryReleaseFailed>(x => x.SetEntityName("inventory:inventory-release-failed"));
        
        config.Message<PaymentIntentCreated>(x => x.SetEntityName("payment:payment-intent-created"));
        config.Message<PaymentIntentFailed>(x => x.SetEntityName("payment:payment-intent-failed"));
        config.Message<PaymentIntentCancelled>(x => x.SetEntityName("payment:payment-intent-cancelled"));
        config.Message<PaymentIntentCancellationFailed>(x => x.SetEntityName("payment:payment-intent-cancellation-failed"));
        config.Message<PaymentConfirmed>(x => x.SetEntityName("payment:payment-confirmed"));
        config.Message<PaymentFailed>(x => x.SetEntityName("payment:payment-failed"));
        config.Message<PaymentRefunded>(x => x.SetEntityName("payment:payment-refunded"));
        config.Message<PaymentRefundFailed>(x => x.SetEntityName("payment:payment-refund-failed"));
        
        config.Message<CoinsHeld>(x => x.SetEntityName("wallet:coins-held"));
        config.Message<CoinsHoldFailed>(x => x.SetEntityName("wallet:coins-hold-failed"));
        config.Message<HoldCancelled>(x => x.SetEntityName("wallet:hold-cancelled"));
        config.Message<HoldCancellationFailed>(x => x.SetEntityName("wallet:hold-cancellation-failed"));
        config.Message<HoldCommitted>(x => x.SetEntityName("wallet:hold-committed"));
        config.Message<HoldCommitFailed>(x => x.SetEntityName("wallet:hold-commit-failed"));
        config.Message<CoinsRefunded>(x => x.SetEntityName("wallet:coins-refunded"));
        config.Message<CoinsRefundFailed>(x => x.SetEntityName("wallet:coins-refund-failed"));
        
        config.Message<OrderPlaced>(x => x.SetEntityName("ordering:order-placed"));
        config.Message<OrderPlacementFailed>(x => x.SetEntityName("ordering:order-placement-failed"));
        config.Message<OrderPaid>(x => x.SetEntityName("ordering:order-paid"));
        config.Message<OrderPaymentFailed>(x => x.SetEntityName("ordering:order-payment-failed"));
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
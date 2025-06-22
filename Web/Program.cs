using MassTransit;
using Web.Checkout.OrderPayment;
using Web.Checkout.OrderPlacement;
using Web.Services.Inventory;
using Web.Services.Ordering;
using Web.Services.Payment;
using Web.Services.Wallet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddSagaStateMachine<OrderPlacementStateMachine, OrderPlacementState>()
        .InMemoryRepository();
    cfg.AddSagaStateMachine<OrderPaymentStateMachine, OrderPaymentState>()
        .InMemoryRepository();
    
    cfg.AddRequestClient<StartOrderPlacementSaga>();
    cfg.AddRequestClient<StartOrderPaymentSaga>();
    
    cfg.AddConsumer<ReserveInventoryConsumer>().Endpoint(c => c.Name = "reserve-inventory");
    cfg.AddConsumer<CancelReservationConsumer>().Endpoint(c => c.Name = "cancel-reservation");
    cfg.AddConsumer<HoldCoinsConsumer>().Endpoint(c => c.Name = "hold-coins");
    cfg.AddConsumer<CancelHoldConsumer>().Endpoint(c => c.Name = "cancel-hold");
    cfg.AddConsumer<CreatePaymentIntentConsumer>().Endpoint(c => c.Name = "create-payment-intent");
    cfg.AddConsumer<CancelPaymentIntentConsumer>().Endpoint(c => c.Name = "cancel-payment-intent");
    cfg.AddConsumer<PlaceOrderConsumer>().Endpoint(c => c.Name = "place-order");
    cfg.AddConsumer<ConfirmPaymentConsumer>().Endpoint(c => c.Name = "confirm-payment");
    cfg.AddConsumer<RefundPaymentConsumer>().Endpoint(c => c.Name = "refund-payment");
    cfg.AddConsumer<MoveOrderToPaidStateConsumer>().Endpoint(c => c.Name = "move-order-to-paid-state");
    
    cfg.UsingInMemory((context, config) =>
    {
        config.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("checkout/run", async (IRequestClient<StartOrderPlacementSaga> requestClient, CancellationToken cancellationToken) =>
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
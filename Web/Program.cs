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
    cfg.AddSagaStateMachine<CheckoutStateMachine, CheckoutState>()
        .InMemoryRepository();
    cfg.AddSagaStateMachine<OrderPaymentStateMachine, OrderPaymentState>()
        .InMemoryRepository();
    
    cfg.AddRequestClient<StartOrderPlacement>();
    cfg.AddRequestClient<ConfirmOrderPayment>();
    
    cfg.AddConsumer<ReserveInventoryConsumer>().Endpoint(c => c.Name = "reserve-inventory");
    cfg.AddConsumer<CancelReservationConsumer>().Endpoint(c => c.Name = "cancel-reservation");
    cfg.AddConsumer<DeductCoinsConsumer>().Endpoint(c => c.Name = "deduct-coins");
    cfg.AddConsumer<RefundCoinsConsumer>().Endpoint(c => c.Name = "refund-coins");
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

app.MapGet("checkout/run", async (IRequestClient<StartOrderPlacement> requestClient, CancellationToken cancellationToken) =>
{
    var command = new StartOrderPlacement(Guid.CreateVersion7(), Guid.CreateVersion7(), 10, 5, []);
    var response = await requestClient
        .GetResponse<CheckoutCompleted, CheckoutFailed>(command, cancellationToken);

    if (response.Is(out Response<CheckoutCompleted>? completed))
    {
        if (completed.Message.IsPaymentConfirmationRequired)
        {
            Console.WriteLine("Checkout: Confirmation required");
            return Results.Ok(completed.Message.OrderId);
        }

        Console.WriteLine("Checkout completed");
        return Results.Ok("Checkout completed");
    }

    if (response.Is(out Response<CheckoutFailed>? failed))
    {
        Console.WriteLine("Checkout failed");
        return Results.BadRequest(failed.Message.Reason);
    }
    
    throw new Exception("Unknown response");
});

app.MapGet("checkout/{orderId}/confirm", async (Guid orderId, IRequestClient<ConfirmOrderPayment> requestClient, 
    CancellationToken cancellationToken) =>
{
    var command = new ConfirmOrderPayment(orderId, Guid.CreateVersion7());
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
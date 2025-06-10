using MassTransit;
using Web.Checkout;
using Web.Services.Inventory;
using Web.Services.Ordering;
using Web.Services.Payment;
using Web.Services.Wallet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// builder.Services.AddTransient<OrderingRoutingSlip>();

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddSagaStateMachine<CheckoutStateMachine, CheckoutState>()
        .InMemoryRepository();
    
    cfg.AddRequestClient<StartCheckout>();
    cfg.AddRequestClient<ConfirmCheckout>();
    
    cfg.AddConsumer<ReserveInventoryConsumer>().Endpoint(c => c.Name = "reserve-inventory");
    cfg.AddConsumer<CancelReservationConsumer>().Endpoint(c => c.Name = "cancel-reservation");
    cfg.AddConsumer<DeductCoinsConsumer>().Endpoint(c => c.Name = "deduct-coins");
    cfg.AddConsumer<RefundCoinsConsumer>().Endpoint(c => c.Name = "refund-coins");
    cfg.AddConsumer<CreatePaymentIntentConsumer>().Endpoint(c => c.Name = "create-payment-intent");
    cfg.AddConsumer<CancelPaymentIntentConsumer>().Endpoint(c => c.Name = "cancel-payment-intent");
    cfg.AddConsumer<PlaceOrderConsumer>().Endpoint(c => c.Name = "place-order");
    cfg.AddConsumer<ConfirmPaymentConsumer>().Endpoint(c => c.Name = "confirm-payment");
    cfg.AddConsumer<RefundPaymentConsumer>().Endpoint(c => c.Name = "refund-payment");
    cfg.AddConsumer<PayOrderConsumer>().Endpoint(c => c.Name = "pay-order");
    
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

app.MapGet("checkout/run", async (IRequestClient<StartCheckout> requestClient, CancellationToken cancellationToken) =>
{
    var command = new StartCheckout(Guid.CreateVersion7(), Guid.CreateVersion7(), 10, 0, []);
    var response = await requestClient
        .GetResponse<CheckoutOrderPlaced, CheckoutCompleted, CheckoutFailed>(command, cancellationToken);

    if (response.Is(out Response<CheckoutOrderPlaced>? succeeded))
    {
        Console.WriteLine("Checkout: Confirmation required");
        return Results.Ok(succeeded.Message.OrderId);
    }

    if (response.Is(out Response<CheckoutCompleted>? completed))
    {
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

app.MapGet("checkout/{orderId}/confirm", async (Guid orderId, IRequestClient<ConfirmCheckout> requestClient, 
    CancellationToken cancellationToken) =>
{
    var command = new ConfirmCheckout(orderId);
    var response = await requestClient.GetResponse<CheckoutCompleted, CheckoutFailed>(command, cancellationToken);

    if (response.Is(out Response<CheckoutCompleted>? succeeded))
    {
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

app.Run();
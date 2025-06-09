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
    
    cfg.AddConsumer<ReserveInventoryConsumer>().Endpoint(c => c.Name = "reserve-inventory");
    cfg.AddConsumer<CancelReservationConsumer>().Endpoint(c => c.Name = "cancel-reservation");
    cfg.AddConsumer<DeductCoinsConsumer>().Endpoint(c => c.Name = "deduct-coins");
    cfg.AddConsumer<RefundCoinsConsumer>().Endpoint(c => c.Name = "refund-coins");
    cfg.AddConsumer<CreatePaymentIntentConsumer>().Endpoint(c => c.Name = "charge-user");
    cfg.AddConsumer<CancelPaymentIntentConsumer>().Endpoint(c => c.Name = "cancel-payment");
    cfg.AddConsumer<PlaceOrderConsumer>().Endpoint(c => c.Name = "place-order");
    
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

app.MapGet("/run", async (IRequestClient<StartCheckout> requestClient, CancellationToken cancellationToken) =>
{
    var command = new StartCheckout(Guid.CreateVersion7(), Guid.CreateVersion7(), 10, 5, []);
    var response = await requestClient.GetResponse<CheckoutSucceeded, CheckoutFailed>(command, cancellationToken);
    
    Console.WriteLine("Checkout completed");
    
    if (response.Is(out Response<CheckoutSucceeded>? succeeded))
        return Results.Ok(succeeded.Message.OrderId);
    if (response.Is(out Response<CheckoutFailed>? failed))
        return Results.BadRequest(failed.Message.Reason);
    throw new Exception("Unknown response");
});

app.Run();
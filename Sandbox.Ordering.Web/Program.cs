using dotenv.net;
using Grafana.OpenTelemetry;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sandbox.Stock.Shared;
using Sandbox.Ordering;
using Sandbox.Ordering.Clients.Cart;
using Sandbox.Ordering.Sagas.OrderConfirmation;
using Sandbox.Ordering.Sagas.OrderConfirmation.EntityFramework;
using Sandbox.Ordering.Sagas.OrderPayment;
using Sandbox.Ordering.Sagas.OrderPayment.EntityFramework;
using Sandbox.Ordering.Sagas.OrderPlacement;
using Sandbox.Ordering.Sagas.OrderPlacement.EntityFramework;
using Sandbox.Ordering.Services;
using Sandbox.Ordering.Shared;
using Sandbox.Payment.Shared;
using Sandbox.Wallet.Shared;

const string serviceName = "Ordering";

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
            .AddHttpClientInstrumentation()
            .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName);
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

builder.Services.AddHttpClient<ICartClient, CartClient>((_, httpClient) =>
{
    var cartUri = builder.Configuration.GetSection("Clients:Cart:Uri").Value;
    if (string.IsNullOrWhiteSpace(cartUri)) throw new Exception("Cart URI is not configured");
    httpClient.BaseAddress = new Uri(cartUri);
}).AddStandardResilienceHandler();
builder.Services.AddTransient<IOrderService, OrderService>();

builder.Services.AddDbContext<OrderPlacementSagaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderingPlacement"));
});
builder.Services.AddDbContext<OrderPaymentSagaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderingPayment"));
});
builder.Services.AddDbContext<OrderConfirmationSagaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderingConfirmation"));
});

builder.Services.AddMassTransit(cfg =>
{
    // Placement
    cfg.AddRequestClient<StartOrderPlacementSaga>();
    cfg.AddSagaStateMachine<OrderPlacementStateMachine, OrderPlacementState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.ExistingDbContext<OrderPlacementSagaDbContext>();
            r.UsePostgres();
        })
        .Endpoint(e =>
        {
            e.Name = "ordering:order-placement-saga-state";
            e.AddConfigureEndpointCallback((context, c) =>
            {
                c.UseEntityFrameworkOutbox<OrderPlacementSagaDbContext>(context);
            });
        });
    cfg.AddEntityFrameworkOutbox<OrderPlacementSagaDbContext>(o =>
    {
        o.UsePostgres();
    });
    
    // Payment
    cfg.AddRequestClient<StartOrderPaymentSaga>();
    cfg.AddSagaStateMachine<OrderPaymentStateMachine, OrderPaymentState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.ExistingDbContext<OrderPaymentSagaDbContext>();
            r.UsePostgres();
        })
        .Endpoint(e =>
        {
            e.Name = "ordering:order-payment-saga-state";
            e.AddConfigureEndpointCallback((context, c) =>
            {
                c.UseEntityFrameworkOutbox<OrderPaymentSagaDbContext>(context);
            });
        });
    cfg.AddEntityFrameworkOutbox<OrderPaymentSagaDbContext>(o =>
    {
        o.UsePostgres();
    });
    
    // Confirmation
    cfg.AddRequestClient<StartOrderConfirmationSaga>();
    cfg.AddSagaStateMachine<OrderConfirmationStateMachine, OrderConfirmationState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.ExistingDbContext<OrderConfirmationSagaDbContext>();
            r.UsePostgres();
        })
        .Endpoint(e =>
        {
            e.Name = "ordering:order-confirmation-saga-state";
            e.AddConfigureEndpointCallback((context, c) =>
            {
                c.UseEntityFrameworkOutbox<OrderConfirmationSagaDbContext>(context);
            });
        });
    cfg.AddEntityFrameworkOutbox<OrderConfirmationSagaDbContext>(o =>
    {
        o.UsePostgres();
    });
    
    cfg.UsingRabbitMq((context, config) =>
    {
        var rabbitMq = builder.Configuration.GetSection("RabbitMQ");
        config.Host(rabbitMq["Host"], ushort.Parse(rabbitMq["Port"]), rabbitMq["VirtualHost"], _ => { });
        config.ConfigureEndpoints(context);
        
        config.Message<StartOrderPlacementSaga>(x => x.SetEntityName("ordering:start-order-placement-saga"));
        config.Message<OrderPlacementSagaCompleted>(x => x.SetEntityName("ordering:order-placement-saga-completed"));
        config.Message<OrderPlacementSagaFailed>(x => x.SetEntityName("ordering:order-placement-saga-failed"));
        
        config.Message<StartOrderPaymentSaga>(x => x.SetEntityName("ordering:start-order-payment-saga"));
        config.Message<OrderPaymentSagaCompleted>(x => x.SetEntityName("ordering:order-payment-saga-completed"));
        config.Message<OrderPaymentSagaFailed>(x => x.SetEntityName("ordering:order-payment-saga-failed"));
        
        config.Message<StartOrderConfirmationSaga>(x => x.SetEntityName("ordering:start-order-confirmation-saga"));
        config.Message<OrderConfirmationSagaCompleted>(x => x.SetEntityName("ordering:order-confirmation-saga-completed"));
        config.Message<OrderConfirmationSagaFailed>(x => x.SetEntityName("ordering:order-confirmation-saga-failed"));
        
        config.Message<StockReserved>(x => x.SetEntityName("stock:stock-reserved"));
        config.Message<StockReservationFailed>(x => x.SetEntityName("stock:stock-reservation-failed"));
        config.Message<StockReleased>(x => x.SetEntityName("stock:stock-released"));
        config.Message<StockReleaseFailed>(x => x.SetEntityName("stock:stock-release-failed"));
        config.Message<StockReservationExtended>(x => x.SetEntityName("stock:stock-reservation-extended"));
        config.Message<StockReservationExtensionFailed>(x => x.SetEntityName("stock:stock-reservation-extension-failed"));
        config.Message<StockReservationReduced>(x => x.SetEntityName("stock:stock-reservation-reduced"));
        config.Message<StockReservationReductionFailed>(x => x.SetEntityName("stock:stock-reservation-reduction-failed"));
        config.Message<StockReservationConfirmed>(x => x.SetEntityName("stock:stock-reservation-confirmed"));
        config.Message<StockReservationConfirmationFailed>(x => x.SetEntityName("stock:stock-reservation-confirmation-failed"));
        config.Message<StockReservationReverted>(x => x.SetEntityName("stock:stock-reservation-reverted"));
        config.Message<StockReservationReversionFailed>(x => x.SetEntityName("stock:stock-reservation-reversion-failed"));
        
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
        config.Message<OrderConfirmed>(x => x.SetEntityName("ordering:order-confirmed"));
        config.Message<OrderConfirmationFailed>(x => x.SetEntityName("ordering:order-confirmation-failed"));
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    await using var orderPlacementDbContext = scope.ServiceProvider.GetRequiredService<OrderPlacementSagaDbContext>();
    await using var orderPaymentDbContext = scope.ServiceProvider.GetRequiredService<OrderPaymentSagaDbContext>();
    await using var orderConfirmationDbContext = scope.ServiceProvider.GetRequiredService<OrderConfirmationSagaDbContext>();
    await Task.WhenAll(
        orderPlacementDbContext.Database.MigrateAsync(),
        orderPaymentDbContext.Database.MigrateAsync(),
        orderConfirmationDbContext.Database.MigrateAsync()
    );
}

app.MapGet("checkout/run", async (IRequestClient<StartOrderPlacementSaga> requestClient, ICartClient cartClient,
    CancellationToken cancellationToken) =>
{
    var cart = await cartClient.GetCartAsync(Guid.CreateVersion7(), cancellationToken);
    
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

app.MapGet("orders/{orderId}/confirm", async (Guid orderId, IRequestClient<StartOrderConfirmationSaga> requestClient,
    CancellationToken cancellationToken) =>
{
    var command = new StartOrderConfirmationSaga(orderId);
    var response = await requestClient
        .GetResponse<OrderConfirmationSagaCompleted, OrderConfirmationSagaFailed>(command, cancellationToken);

    if (response.Is(out Response<OrderConfirmationSagaCompleted>? succeeded))
    {
        Console.WriteLine("Order confirmed");
        return Results.Ok("Order confirmed");
    }

    if (response.Is(out Response<OrderConfirmationSagaFailed>? failed))
    {
        Console.WriteLine("Order confirmation failed");
        return Results.BadRequest(failed.Message.Reason);
    }
    
    throw new Exception("Unknown response");
});

app.Run();
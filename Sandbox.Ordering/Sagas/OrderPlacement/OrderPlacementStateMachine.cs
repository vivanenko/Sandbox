using MassTransit;
using Sandbox.Stock.Shared;
using Sandbox.Ordering.Shared;
using Sandbox.Payment.Shared;
using Sandbox.Wallet.Shared;

namespace Sandbox.Ordering.Sagas.OrderPlacement;

public class OrderPlacementState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public string CurrentState { get; set; }

    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }

    public int CoinsAmount { get; set; }
    public decimal Amount { get; set; }
    public ItemDto[] Items { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    
    public Guid RequestId { get; set; }
    public Uri ResponseAddress { get; set; }
}

public class OrderPlacementStateMachine : MassTransitStateMachine<OrderPlacementState>
{
    public State AwaitingStockReservation { get; private set; }
    public State AwaitingCoinsHold { get; private set; }
    public State AwaitingPaymentIntent { get; private set; }
    public State AwaitingOrderPlacement { get; private set; }
    public State AwaitingStockReservationCancellation { get; private set; }
    public State AwaitingCoinsHoldCancellation { get; private set; }
    public State AwaitingPaymentIntentCancellation { get; private set; }

    public Event<StartOrderPlacementSaga> StartOrderPlacement { get; private set; }
    public Event<OrderPlacementSagaCompleted> OrderPlacementSagaCompleted { get; private set; }
    public Event<OrderPlacementSagaFailed> OrderPlacementSagaFailed { get; private set; }
    
    public Event<StockReserved> StockReserved { get; private set; }
    public Event<StockReservationFailed> StockReservationFailed { get; private set; }
    public Event<StockReleased> StockReleased { get; private set; }
    public Event<StockReleaseFailed> StockReleaseFailed { get; private set; }

    public Event<CoinsHeld> CoinsHeld { get; private set; }
    public Event<CoinsHoldFailed> CoinsHoldFailed { get; private set; }
    public Event<HoldCancelled> HoldCancelled { get; private set; }
    public Event<HoldCancellationFailed> HoldCancellationFailed { get; private set; }

    public Event<PaymentIntentCreated> PaymentIntentCreated { get; private set; }
    public Event<PaymentIntentFailed> PaymentIntentFailed { get; private set; }
    public Event<PaymentIntentCancelled> PaymentIntentCancelled { get; private set; }
    public Event<PaymentIntentCancellationFailed> PaymentIntentCancellationFailed { get; private set; }

    public Event<OrderPlaced> OrderPlaced { get; private set; }
    public Event<OrderPlacementFailed> OrderPlacementFailed { get; private set; }
    
    public OrderPlacementStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => StartOrderPlacement, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPlacementSagaCompleted, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPlacementSagaFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => StockReserved, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReleased, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReleaseFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => CoinsHeld, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CoinsHoldFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => HoldCancelled, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => HoldCancellationFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => PaymentIntentCreated, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentIntentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentIntentCancelled, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentIntentCancellationFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => OrderPlaced, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPlacementFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Initially(
            When(StartOrderPlacement)
                .Then(context =>
                {
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.Amount = context.Message.Amount;
                    context.Saga.CoinsAmount = context.Message.CoinsAmount;
                    context.Saga.Items = context.Message.Items;

                    if (!context.RequestId.HasValue || context.ResponseAddress is null)
                        throw new Exception("RequestId and ResponseAddress are required");
                    
                    context.Saga.RequestId = context.RequestId.Value;
                    context.Saga.ResponseAddress = context.ResponseAddress;
                })
                .Send(new Uri("queue:stock:reserve-stock"), context => new ReserveStock(context.Saga.OrderId, context.Saga.Items))
                .TransitionTo(AwaitingStockReservation)
        );

        During(AwaitingStockReservation,
            When(StockReserved)
                .IfElse(context => context.Saga.CoinsAmount > 0,
                    binder => binder.Send(new Uri("queue:wallet:hold-coins"), context => new HoldCoins(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                                    .TransitionTo(AwaitingCoinsHold),
                    binder => binder.IfElse(context => context.Saga.Amount > 0,
                        innerBinder => innerBinder
                            .Send(new Uri("queue:payment:create-payment-intent"), context => new CreatePaymentIntent(context.Saga.OrderId, context.Saga.UserId, context.Saga.Amount))
                            .TransitionTo(AwaitingPaymentIntent),
                        innerBinder => innerBinder
                            .Send(new Uri("queue:ordering:place-order"), context => new PlaceOrder(context.Saga.OrderId))
                            .TransitionTo(AwaitingOrderPlacement)
                    )
                ),

            When(StockReservationFailed)
                .ThenAsync(async context =>
                {
                    var message = new OrderPlacementSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize()
        );

        During(AwaitingStockReservationCancellation,
            When(StockReleased)
                .ThenAsync(async context =>
                {
                    var message = new OrderPlacementSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize(),
            
            When(StockReleaseFailed)
                .ThenAsync(async context =>
                {
                    var message = new OrderPlacementSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize()
        );

        During(AwaitingCoinsHold,
            When(CoinsHeld)
                .IfElse(context => context.Saga.Amount > 0,
                    binder => binder.Send(new Uri("queue:payment:create-payment-intent"), context => new CreatePaymentIntent(context.Saga.OrderId, context.Saga.UserId, context.Saga.Amount))
                                    .TransitionTo(AwaitingPaymentIntent),
                    binder => binder.Send(new Uri("queue:ordering:place-order"), context => new PlaceOrder(context.Saga.OrderId))
                                    .TransitionTo(AwaitingOrderPlacement)
                ),

            When(CoinsHoldFailed)
                .Send(new Uri("queue:stock:release-stock"), context => new ReleaseStock(context.Saga.OrderId))
                .TransitionTo(AwaitingStockReservationCancellation)
        );
        
        During(AwaitingCoinsHoldCancellation,
            When(HoldCancelled)
                .Send(new Uri("queue:stock:release-stock"), context => new ReleaseStock(context.Saga.OrderId))
                .TransitionTo(AwaitingStockReservationCancellation),
            
            When(HoldCancellationFailed)
                .Send(new Uri("queue:stock:release-stock"), context => new ReleaseStock(context.Saga.OrderId))
                .TransitionTo(AwaitingStockReservationCancellation)
        );

        During(AwaitingPaymentIntent,
            When(PaymentIntentCreated)
                .Send(new Uri("queue:ordering:place-order"), context => new PlaceOrder(context.Saga.OrderId))
                .TransitionTo(AwaitingOrderPlacement),

            When(PaymentIntentFailed)
                .If(context => context.Saga.CoinsAmount > 0,
                    binder => binder.Send(new Uri("queue:wallet:cancel-hold"), context => new CancelHold(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                                    .TransitionTo(AwaitingCoinsHoldCancellation)
                )
                .Send(new Uri("queue:stock:release-stock"), context => new ReleaseStock(context.Saga.OrderId))
                .TransitionTo(AwaitingStockReservationCancellation)
        );
        
        During(AwaitingPaymentIntentCancellation,
            When(PaymentIntentCancelled)
                .IfElse(context => context.Saga.CoinsAmount > 0, 
                    binder => binder
                        .Send(new Uri("queue:wallet:cancel-hold"), context => new CancelHold(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                        .TransitionTo(AwaitingCoinsHoldCancellation),
                    binder => binder
                        .Send(new Uri("queue:stock:release-stock"), context => new ReleaseStock(context.Saga.OrderId))
                        .TransitionTo(AwaitingStockReservationCancellation)
                ),
            
            When(PaymentIntentCancellationFailed)
                .IfElse(context => context.Saga.CoinsAmount > 0, 
                    binder => binder
                        .Send(new Uri("queue:wallet:cancel-hold"), context => new CancelHold(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                        .TransitionTo(AwaitingCoinsHoldCancellation),
                    binder => binder
                        .Send(new Uri("queue:stock:release-stock"), context => new ReleaseStock(context.Saga.OrderId))
                        .TransitionTo(AwaitingStockReservationCancellation)
                )
        );

        During(AwaitingOrderPlacement,
            When(OrderPlaced)
                .ThenAsync(async context =>
                {
                    var message = new OrderPlacementSagaCompleted(context.Saga.OrderId, true);
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize(),
            
            When(OrderPlacementFailed)
                .IfElse(context => context.Saga.Amount > 0,
                    binder => binder.Send(new Uri("queue:payment:cancel-payment-intent"), context => new CancelPaymentIntent(context.Saga.OrderId))
                                    .TransitionTo(AwaitingPaymentIntentCancellation),
                    binder => binder.IfElse(context => context.Saga.CoinsAmount > 0,
                        innerBinder => innerBinder
                            .Send(new Uri("queue:wallet:cancel-hold"), context => new CancelHold(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                            .TransitionTo(AwaitingCoinsHoldCancellation),
                        innerBinder => innerBinder
                            .Send(new Uri("queue:stock:release-stock"), context => new ReleaseStock(context.Saga.OrderId))
                            .TransitionTo(AwaitingStockReservationCancellation)
                        )
                )
        );

        SetCompletedWhenFinalized();
    }
}
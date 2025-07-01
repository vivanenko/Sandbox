using MassTransit;
using Sandbox.Inventory.Shared;
using Sandbox.Ordering.Shared;
using Sandbox.Payment.Shared;
using Sandbox.Wallet.Shared;

namespace Sandbox.Ordering.Sagas.OrderPlacement;

public class OrderPlacementState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public int Version { get; set; }
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
    public State WaitingForInventory { get; private set; }
    public State WaitingForCoinsHold { get; private set; }
    public State WaitingForPaymentIntent { get; private set; }
    public State WaitingForOrderPlacement { get; private set; }
    public State WaitingForReservationCancellation { get; private set; }
    public State WaitingForHoldCancellation { get; private set; }
    public State WaitingForPaymentCancellation { get; private set; }

    public Event<StartOrderPlacementSaga> StartOrderPlacement { get; private set; }
    public Event<OrderPlacementSagaCompleted> OrderPlacementSagaCompleted { get; private set; }
    public Event<OrderPlacementSagaFailed> OrderPlacementSagaFailed { get; private set; }
    
    public Event<InventoryReserved> InventoryReserved { get; private set; }
    public Event<InventoryReservationFailed> InventoryReservationFailed { get; private set; }
    public Event<InventoryReleased> InventoryReleased { get; private set; }
    public Event<InventoryReleaseFailed> InventoryReleaseFailed { get; private set; }

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
        
        Event(() => InventoryReserved, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReservationFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReleased, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReleaseFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
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
                .Send(new Uri("queue:inventory:reserve-inventory"), context => new ReserveInventory(context.Saga.OrderId, context.Saga.Items))
                .TransitionTo(WaitingForInventory)
        );

        During(WaitingForInventory,
            When(InventoryReserved)
                .IfElse(context => context.Saga.CoinsAmount > 0,
                    binder => binder.Send(new Uri("queue:wallet:hold-coins"), context => new HoldCoins(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                                    .TransitionTo(WaitingForCoinsHold),
                    binder => binder.IfElse(context => context.Saga.Amount > 0,
                        innerBinder => innerBinder
                            .Send(new Uri("queue:payment:create-payment-intent"), context => new CreatePaymentIntent(context.Saga.OrderId, context.Saga.UserId, context.Saga.Amount))
                            .TransitionTo(WaitingForPaymentIntent),
                        innerBinder => innerBinder
                            .Send(new Uri("queue:ordering:place-order"), context => new PlaceOrder(context.Saga.OrderId))
                            .TransitionTo(WaitingForOrderPlacement)
                    )
                ),

            When(InventoryReservationFailed)
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

        During(WaitingForReservationCancellation,
            When(InventoryReleased)
                .ThenAsync(async context =>
                {
                    var message = new OrderPlacementSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize(),
            
            When(InventoryReleaseFailed)
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

        During(WaitingForCoinsHold,
            When(CoinsHeld)
                .IfElse(context => context.Saga.Amount > 0,
                    binder => binder.Send(new Uri("queue:payment:create-payment-intent"), context => new CreatePaymentIntent(context.Saga.OrderId, context.Saga.UserId, context.Saga.Amount))
                                    .TransitionTo(WaitingForPaymentIntent),
                    binder => binder.Send(new Uri("queue:ordering:place-order"), context => new PlaceOrder(context.Saga.OrderId))
                                    .TransitionTo(WaitingForOrderPlacement)
                ),

            When(CoinsHoldFailed)
                .Send(new Uri("queue:inventory:release-inventory"), context => new ReleaseInventory(context.Saga.OrderId))
                .TransitionTo(WaitingForReservationCancellation)
        );
        
        During(WaitingForHoldCancellation,
            When(HoldCancelled)
                .Send(new Uri("queue:inventory:release-inventory"), context => new ReleaseInventory(context.Saga.OrderId))
                .TransitionTo(WaitingForReservationCancellation),
            
            When(HoldCancellationFailed)
                .Send(new Uri("queue:inventory:release-inventory"), context => new ReleaseInventory(context.Saga.OrderId))
                .TransitionTo(WaitingForReservationCancellation)
        );

        During(WaitingForPaymentIntent,
            When(PaymentIntentCreated)
                .Send(new Uri("queue:ordering:place-order"), context => new PlaceOrder(context.Saga.OrderId))
                .TransitionTo(WaitingForOrderPlacement),

            When(PaymentIntentFailed)
                .If(context => context.Saga.CoinsAmount > 0,
                    binder => binder.Send(new Uri("queue:wallet:cancel-hold"), context => new CancelHold(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                                    .TransitionTo(WaitingForHoldCancellation)
                )
                .Send(new Uri("queue:inventory:release-inventory"), context => new ReleaseInventory(context.Saga.OrderId))
                .TransitionTo(WaitingForReservationCancellation)
        );
        
        During(WaitingForPaymentCancellation,
            When(PaymentIntentCancelled)
                .IfElse(context => context.Saga.CoinsAmount > 0, 
                    binder => binder
                        .Send(new Uri("queue:wallet:cancel-hold"), context => new CancelHold(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                        .TransitionTo(WaitingForHoldCancellation),
                    binder => binder
                        .Send(new Uri("queue:inventory:release-inventory"), context => new ReleaseInventory(context.Saga.OrderId))
                        .TransitionTo(WaitingForReservationCancellation)
                ),
            
            When(PaymentIntentCancellationFailed)
                .IfElse(context => context.Saga.CoinsAmount > 0, 
                    binder => binder
                        .Send(new Uri("queue:wallet:cancel-hold"), context => new CancelHold(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                        .TransitionTo(WaitingForHoldCancellation),
                    binder => binder
                        .Send(new Uri("queue:inventory:release-inventory"), context => new ReleaseInventory(context.Saga.OrderId))
                        .TransitionTo(WaitingForReservationCancellation)
                )
        );

        During(WaitingForOrderPlacement,
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
                                    .TransitionTo(WaitingForPaymentCancellation),
                    binder => binder.IfElse(context => context.Saga.CoinsAmount > 0,
                        innerBinder => innerBinder
                            .Send(new Uri("queue:wallet:cancel-hold"), context => new CancelHold(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                            .TransitionTo(WaitingForHoldCancellation),
                        innerBinder => innerBinder
                            .Send(new Uri("queue:inventory:release-inventory"), context => new ReleaseInventory(context.Saga.OrderId))
                            .TransitionTo(WaitingForReservationCancellation)
                        )
                )
        );

        SetCompletedWhenFinalized();
    }
}
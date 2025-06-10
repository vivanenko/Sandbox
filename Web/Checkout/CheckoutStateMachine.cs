using MassTransit;
using Web.Common;
using Web.Services.Inventory;
using Web.Services.Ordering;
using Web.Services.Payment;
using Web.Services.Wallet;

namespace Web.Checkout;

public class CheckoutState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }

    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }

    public int CoinsAmount { get; set; }
    public decimal Amount { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public bool IsInventoryReservationFailed { get; set; }
    public bool IsInventoryReservationCancelled { get; set; }

    public bool IsCoinsDeducted { get; set; }
    public bool IsCoinsDeductionFailed { get; set; }
    public bool IsCoinsRefunded { get; set; }

    public bool IsPaymentIntentCreated { get; set; }
    public bool IsPaymentIntentFailed { get; set; }
    public bool IsPaymentIntentCancelled { get; set; }
    
    public bool IsOrderPlacementFailed { get; set; }

    private bool IsAnyTransactionFailed => IsInventoryReservationFailed ||
                                           IsCoinsDeductionFailed ||
                                           IsPaymentIntentFailed ||
                                           IsOrderPlacementFailed;
    private bool IsAllTransactionsCompensated => IsInventoryReservationCancelled &&
                                                 (!IsCoinsDeducted || IsCoinsRefunded) &&
                                                 (!IsPaymentIntentCreated || IsPaymentIntentCancelled);
    public bool IsCompensated => IsAnyTransactionFailed && IsAllTransactionsCompensated;

    public Guid StartCheckoutRequestId { get; set; }
    public Uri StartCheckoutResponseAddress { get; set; }
    
    public Guid ConfirmCheckoutRequestId { get; set; }
    public Uri ConfirmCheckoutResponseAddress { get; set; }

    public bool IsPaymentConfirmationRequired => Amount > 0;
}

public class CheckoutStateMachine : MassTransitStateMachine<CheckoutState>
{
    public State WaitingForInventory { get; private set; }
    public State WaitingForCoinsDeduction { get; private set; }
    public State WaitingForPaymentIntent { get; private set; }
    public State WaitingForOrderPlacement { get; private set; }
    public State WaitingForCheckoutConfirmation { get; private set; }
    public State WaitingForPaymentConfirmation { get; private set; }
    public State WaitingForOrderPayment { get; private set; }
    public State Failed { get; private set; }

    public Event<StartCheckout> StartCheckout { get; private set; }
    public Event<CheckoutOrderPlaced> CheckoutOrderPlaced { get; private set; }
    public Event<ConfirmCheckout> ConfirmCheckout { get; private set; }
    public Event<CheckoutCompleted> CheckoutCompleted { get; private set; }
    public Event<CheckoutFailed> CheckoutFailed { get; private set; }
    
    public Event<InventoryReserved> InventoryReserved { get; private set; }
    public Event<InventoryReservationFailed> InventoryReservationFailed { get; private set; }
    public Event<InventoryReservationCancelled> InventoryReservationCancelled { get; private set; }

    public Event<CoinsDeducted> CoinsDeducted { get; private set; }
    public Event<CoinsDeductionFailed> CoinsDeductionFailed { get; private set; }
    public Event<CoinsRefunded> CoinsRefunded { get; private set; }

    public Event<PaymentIntentCreated> PaymentIntentCreated { get; private set; }
    public Event<PaymentIntentFailed> PaymentIntentFailed { get; private set; }
    public Event<PaymentIntentCancelled> PaymentIntentCancelled { get; private set; }

    public Event<OrderPlaced> OrderPlaced { get; private set; }
    public Event<OrderPlacementFailed> OrderPlacementFailed { get; private set; }
    
    public Event<PaymentConfirmed> PaymentConfirmed { get; private set; }
    public Event<PaymentFailed> PaymentFailed { get; private set; }
    public Event<PaymentRefunded> PaymentRefunded { get; private set; }
    
    public Event<OrderPaid> OrderPaid { get; private set; }
    public Event<OrderPaymentFailed> OrderPaymentFailed { get; private set; }
    
    public CheckoutStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => StartCheckout, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CheckoutOrderPlaced, x => x.CorrelateById(context => context.Message.OrderId)); // ?
        Event(() => ConfirmCheckout, x => x.CorrelateById(context => context.Message.OrderId)); // ?
        Event(() => CheckoutCompleted, x => x.CorrelateById(context => context.Message.OrderId)); // ?
        Event(() => CheckoutFailed, x => x.CorrelateById(context => context.Message.OrderId)); // ?
        
        Event(() => InventoryReserved, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReservationFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReservationCancelled, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => CoinsDeducted, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CoinsDeductionFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CoinsRefunded, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => PaymentIntentCreated, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentIntentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentIntentCancelled, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => OrderPlaced, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPlacementFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => PaymentConfirmed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentRefunded, x => x.CorrelateById(context => context.Message.OrderId));

        Event(() => OrderPaid, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPaymentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Initially(
            When(StartCheckout)
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
                    
                    context.Saga.StartCheckoutRequestId = context.RequestId.Value;
                    context.Saga.StartCheckoutResponseAddress = context.ResponseAddress;
                })
                .Send(new Uri("queue:reserve-inventory"), context => new ReserveInventory(context.Saga.OrderId, context.Saga.Items))
                .TransitionTo(WaitingForInventory)
        );

        During(WaitingForInventory,
            When(InventoryReserved)
                .IfElse(context => context.Saga.CoinsAmount > 0,
                    binder => binder.Send(new Uri("queue:deduct-coins"), context => new DeductCoins(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                                    .TransitionTo(WaitingForCoinsDeduction),
                    binder => binder.IfElse(context => context.Saga.Amount > 0,
                        innerBinder => innerBinder.Send(new Uri("queue:create-payment-intent"), context => new CreatePaymentIntent(context.Saga.OrderId, context.Saga.UserId, context.Saga.Amount)).TransitionTo(WaitingForPaymentIntent),
                        innerBinder => innerBinder.Send(new Uri("queue:place-order"), context => new PlaceOrder(context.Saga.OrderId)).TransitionTo(WaitingForOrderPlacement)
                    )
                ),

            When(InventoryReservationFailed)
                .Then(context => context.Saga.IsInventoryReservationFailed = true)
                .TransitionTo(Failed)
        );

        During(WaitingForCoinsDeduction,
            When(CoinsDeducted)
                .Then(context => context.Saga.IsCoinsDeducted = true)
                .IfElse(context => context.Saga.Amount > 0,
                    binder => binder.Send(new Uri("queue:create-payment-intent"), context => new CreatePaymentIntent(context.Saga.OrderId, context.Saga.UserId, context.Saga.Amount))
                                    .TransitionTo(WaitingForPaymentIntent),
                    binder => binder.Send(new Uri("queue:place-order"), context => new PlaceOrder(context.Saga.OrderId))
                                    .TransitionTo(WaitingForOrderPlacement)
                ),

            When(CoinsDeductionFailed)
                .Then(context => context.Saga.IsCoinsDeductionFailed = true)
                .Send(new Uri("queue:cancel-reservation"), context => new CancelReservation(context.Saga.OrderId))
                .TransitionTo(Failed)
        );

        During(WaitingForPaymentIntent,
            When(PaymentIntentCreated)
                .Then(context => context.Saga.IsPaymentIntentCreated = true)
                .Send(new Uri("queue:place-order"), context => new PlaceOrder(context.Saga.OrderId))
                .TransitionTo(WaitingForOrderPlacement),

            When(PaymentIntentFailed)
                .Then(context => context.Saga.IsPaymentIntentFailed = true)
                .If(context => context.Saga.IsCoinsDeducted,
                    binder => binder.Send(new Uri("queue:refund-coins"), context => new RefundCoins(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                )
                .Send(new Uri("queue:cancel-reservation"), context => new CancelReservation(context.Saga.OrderId))
                .TransitionTo(Failed)
        );

        During(WaitingForOrderPlacement,
            When(OrderPlaced, context => context.Saga.IsPaymentConfirmationRequired)
                .ThenAsync(async context =>
                {
                    var message = new CheckoutOrderPlaced(context.Saga.OrderId);
                    await context.Send(context.Saga.StartCheckoutResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.StartCheckoutRequestId;
                    });
                })
                .TransitionTo(WaitingForCheckoutConfirmation),
            
            When(OrderPlaced, context => !context.Saga.IsPaymentConfirmationRequired)
                .Send(new Uri("queue:pay-order"), context => new PayOrder(context.Saga.OrderId))
                .TransitionTo(WaitingForOrderPayment),
            
            When(OrderPlacementFailed)
                .Then(context => context.Saga.IsOrderPlacementFailed = true)
                .Send(new Uri("queue:cancel-reservation"), context => new CancelReservation(context.Saga.OrderId))
                .If(context => context.Saga.IsCoinsDeducted,
                    binder => binder.Send(new Uri("queue:refund-coins"), context => new RefundCoins(context.Saga.OrderId, context.Saga.UserId, context.Saga.CoinsAmount))
                )
                .If(context => context.Saga.IsPaymentIntentCreated,
                    binder => binder.Send(new Uri("queue:cancel-payment-intent"), context => new CancelPaymentIntent(context.Saga.OrderId))
                )
                .TransitionTo(Failed)
        );

        During(WaitingForCheckoutConfirmation,
            When(ConfirmCheckout)
                .Then(context =>
                {
                    if (!context.RequestId.HasValue || context.ResponseAddress is null)
                        throw new Exception("RequestId and ResponseAddress are required");
                    
                    context.Saga.ConfirmCheckoutRequestId = context.RequestId.Value;
                    context.Saga.ConfirmCheckoutResponseAddress = context.ResponseAddress;
                })
                .Send(new Uri("queue:confirm-payment"), context => new ConfirmPayment(context.Saga.OrderId))
                .TransitionTo(WaitingForPaymentConfirmation)
        );
        
        During(WaitingForPaymentConfirmation,
            When(PaymentConfirmed)
                .Send(new Uri("queue:pay-order"), context => new PayOrder(context.Saga.OrderId))
                .TransitionTo(WaitingForOrderPayment)
        );
        
        During(WaitingForOrderPayment,
            When(OrderPaid)
                .ThenAsync(async context =>
                {
                    Uri responseAddress;
                    Guid requestId;
                    if (context.Saga.IsPaymentConfirmationRequired)
                    {
                        responseAddress = context.Saga.ConfirmCheckoutResponseAddress;
                        requestId = context.Saga.ConfirmCheckoutRequestId;
                    }
                    else
                    {
                        responseAddress = context.Saga.StartCheckoutResponseAddress;
                        requestId = context.Saga.StartCheckoutRequestId;
                    }
                    var message = new CheckoutCompleted(context.Saga.OrderId);
                    await context.Send(responseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = requestId;
                    });
                })
                .Finalize(),
            
            When(OrderPaymentFailed)
                // todo: Refund
        );
        
        During(Failed,
            When(InventoryReservationCancelled)
                .Then(context => context.Saga.IsInventoryReservationCancelled = true)
                .If(context => context.Saga.IsCompensated, binder =>
                    binder
                        .ThenAsync(async context =>
                        {
                            var message = new CheckoutFailed(context.Saga.OrderId, "");
                            await context.Send(context.Saga.StartCheckoutResponseAddress, message, sendContext =>
                            {
                                sendContext.RequestId = context.Saga.StartCheckoutRequestId;
                            });
                        })
                        .Finalize()),

            When(CoinsRefunded)
                .Then(context => context.Saga.IsCoinsRefunded = true)
                .If(context => context.Saga.IsCompensated, binder =>
                    binder
                        .ThenAsync(async context =>
                        {
                            var message = new CheckoutFailed(context.Saga.OrderId, "");
                            await context.Send(context.Saga.StartCheckoutResponseAddress, message, sendContext =>
                            {
                                sendContext.RequestId = context.Saga.StartCheckoutRequestId;
                            });
                        })
                        .Finalize()),

            When(PaymentIntentCancelled)
                .Then(context => context.Saga.IsPaymentIntentCancelled = true)
                .If(context => context.Saga.IsCompensated, binder =>
                    binder
                        .ThenAsync(async context =>
                        {
                            var message = new CheckoutFailed(context.Saga.OrderId, "");
                            await context.Send(context.Saga.StartCheckoutResponseAddress, message, sendContext =>
                            {
                                sendContext.RequestId = context.Saga.StartCheckoutRequestId;
                            });
                        })
                        .Finalize())
        );
        
        SetCompletedWhenFinalized();
    }
}
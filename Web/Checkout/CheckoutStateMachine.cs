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

    public int BonusPoints { get; set; }
    public decimal Amount { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public bool IsInventoryReservationFailed { get; set; }
    public bool IsInventoryReservationCancelled { get; set; }
    
    public bool IsCoinsDeductionFailed { get; set; }
    public bool IsCoinsRefunded { get; set; }
    
    public bool IsPaymentFailed { get; set; }
    public bool IsPaymentCancelled { get; set; }
    
    public bool IsOrderPlacementFailed { get; set; }

    private bool IsAnyTransactionFailed => IsInventoryReservationFailed ||
                                           IsCoinsDeductionFailed ||
                                           IsPaymentFailed ||
                                           IsOrderPlacementFailed;
    private bool IsAllTransactionsCompensated => IsInventoryReservationCancelled && 
                                                 IsCoinsRefunded &&
                                                 IsPaymentCancelled;
    public bool IsCompensated => IsAnyTransactionFailed && IsAllTransactionsCompensated;

    public Guid RequestId { get; set; }
    public Uri ResponseAddress { get; set; }
}

public class CheckoutStateMachine : MassTransitStateMachine<CheckoutState>
{
    public State WaitingForInventory { get; private set; }
    public State WaitingForCoinsDeduction { get; private set; }
    public State WaitingForPayment { get; private set; }
    public State Paid { get; private set; }
    public State Completed { get; private set; }
    public State Failed { get; private set; }

    public Event<StartCheckout> CheckoutStarted { get; private set; }
    public Event<CheckoutSucceeded> CheckoutSucceeded { get; private set; }
    public Event<CheckoutFailed> CheckoutFailed { get; private set; }
    
    public Event<InventoryReserved> InventoryReserved { get; private set; }
    public Event<InventoryReservationFailed> InventoryReservationFailed { get; private set; }
    public Event<InventoryReservationCancelled> InventoryReservationCancelled { get; private set; }

    public Event<CoinsDeducted> CoinsDeducted { get; private set; }
    public Event<CoinsDeductionFailed> CoinsDeductionFailed { get; private set; }
    public Event<CoinsRefunded> CoinsRefunded { get; private set; }

    public Event<PaymentCharged> PaymentCharged { get; private set; }
    public Event<PaymentFailed> PaymentFailed { get; private set; }
    public Event<PaymentCancelled> PaymentCancelled { get; private set; }

    public Event<OrderPlaced> OrderPlaced { get; private set; }
    public Event<OrderPlacementFailed> OrderPlacementFailed { get; private set; }
    
    public CheckoutStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => CheckoutStarted, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CheckoutSucceeded, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CheckoutFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => InventoryReserved, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReservationFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReservationCancelled, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => CoinsDeducted, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CoinsDeductionFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CoinsRefunded, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => PaymentCharged, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentCancelled, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => OrderPlaced, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPlacementFailed, x => x.CorrelateById(context => context.Message.OrderId));

        Initially(
            When(CheckoutStarted)
                .Then(context =>
                {
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.Amount = context.Message.Amount;
                    context.Saga.BonusPoints = context.Message.CoinsAmount;
                    context.Saga.Items = context.Message.Items;

                    if (!context.RequestId.HasValue || context.ResponseAddress is null)
                        throw new Exception("RequestId and ResponseAddress are required");
                    
                    context.Saga.RequestId = context.RequestId.Value;
                    context.Saga.ResponseAddress = context.ResponseAddress;
                })
                .Send(new Uri("queue:reserve-inventory"), 
                    context => new ReserveInventory(context.Saga.OrderId, context.Saga.Items))
                .TransitionTo(WaitingForInventory)
        );

        During(WaitingForInventory,
            When(InventoryReserved)
                .Send(new Uri("queue:deduct-coins"), 
                    context => new DeductCoins(context.Saga.OrderId, context.Saga.UserId, context.Saga.BonusPoints))
                .TransitionTo(WaitingForCoinsDeduction),

            When(InventoryReservationFailed)
                .Then(context => context.Saga.IsInventoryReservationFailed = true)
                .TransitionTo(Failed)
        );

        During(WaitingForCoinsDeduction,
            When(CoinsDeducted)
                .Send(new Uri("queue:charge-user"), 
                    context => new ChargeUser(context.Saga.OrderId, context.Saga.UserId, context.Saga.Amount))
                .TransitionTo(WaitingForPayment),

            When(CoinsDeductionFailed)
                .Then(context => context.Saga.IsCoinsDeductionFailed = true)
                .Send(new Uri("queue:cancel-reservation"), context => new CancelReservation(context.Saga.OrderId))
                .TransitionTo(Failed)
        );

        During(WaitingForPayment,
            When(PaymentCharged)
                .Send(new Uri("queue:place-order"), context => new PlaceOrder(context.Saga.OrderId))
                .TransitionTo(Paid),

            When(PaymentFailed)
                .Then(context => context.Saga.IsPaymentFailed = true)
                .Send(new Uri("queue:refund-coins"), 
                    context => new RefundCoins(context.Saga.OrderId, context.Saga.UserId, context.Saga.BonusPoints))
                .Send(new Uri("queue:cancel-reservation"), context => new CancelReservation(context.Saga.OrderId))
                .TransitionTo(Failed)
        );

        During(Paid,
            When(OrderPlaced)
                .ThenAsync(async context =>
                {
                    var message = new CheckoutSucceeded(context.Saga.OrderId);
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .TransitionTo(Completed)
                .Finalize(),
            
            When(OrderPlacementFailed)
                .Then(context => context.Saga.IsOrderPlacementFailed = true)
                .Send(new Uri("queue:refund-coins"), 
                    context => new RefundCoins(context.Saga.OrderId, context.Saga.UserId, context.Saga.BonusPoints))
                .Send(new Uri("queue:cancel-reservation"), context => new CancelReservation(context.Saga.OrderId))
                .Send(new Uri("queue:cancel-payment"), context => new CancelPayment(context.Saga.OrderId))
                .TransitionTo(Failed)
        );
        
        During(Failed,
            When(InventoryReservationCancelled)
                .Then(context => context.Saga.IsInventoryReservationCancelled = true)
                .If(context => context.Saga.IsCompensated, binder =>
                    binder
                        .ThenAsync(async context =>
                        {
                            var message = new CheckoutFailed(context.Saga.OrderId, "");
                            await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                            {
                                sendContext.RequestId = context.Saga.RequestId;
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
                            await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                            {
                                sendContext.RequestId = context.Saga.RequestId;
                            });
                        })
                        .Finalize()),

            When(PaymentCancelled)
                .Then(context => context.Saga.IsPaymentCancelled = true)
                .If(context => context.Saga.IsCompensated, binder =>
                    binder
                        .ThenAsync(async context =>
                        {
                            var message = new CheckoutFailed(context.Saga.OrderId, "");
                            await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                            {
                                sendContext.RequestId = context.Saga.RequestId;
                            });
                        })
                        .Finalize())
        );
        
        SetCompletedWhenFinalized();
    }
}
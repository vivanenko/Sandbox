using MassTransit;
using Sandbox.Stock.Shared;
using Sandbox.Ordering.Shared;
using Sandbox.Payment.Shared;
using Sandbox.Wallet.Shared;

namespace Sandbox.Ordering.Sagas.OrderPayment;

public class OrderPaymentState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public string CurrentState { get; set; }
    
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    
    public Guid RequestId { get; set; }
    public Uri ResponseAddress { get; set; }
}

public class OrderPaymentStateMachine : MassTransitStateMachine<OrderPaymentState>
{
    public State AwaitingStockReservationExtension { get; private set; }
    public State AwaitingCoinsHoldCommit { get; private set; }
    public State AwaitingPaymentConfirmation { get; private set; }
    public State AwaitingOrderPayment { get; private set; }
    public State AwaitingPaymentRefund { get; private set; }
    public State AwaitingCoinsRefund { get; private set; }
    public State AwaitingStockReservationReduction { get; private set; }
    
    public Event<StartOrderPaymentSaga> StartOrderPaymentSaga { get; private set; }
    public Event<OrderPaymentSagaFailed> OrderPaymentSagaFailed { get; private set; }
    public Event<OrderPaymentSagaCompleted> OrderPaymentSagaCompleted { get; private set; }

    public Event<StockReservationExtended> StockReservationExtended { get; private set; }
    public Event<StockReservationExtensionFailed> StockReservationExtensionFailed { get; private set; }
    public Event<StockReservationReduced> StockReservationReduced { get; private set; }
    public Event<StockReservationReductionFailed> StockReservationReductionFailed { get; private set; }
    
    public Event<HoldCommitted> HoldCommitted { get; set; }
    public Event<HoldCommitFailed> HoldCommitFailed { get; set; }
    public Event<CoinsRefunded> CoinsRefunded { get; set; }
    public Event<CoinsRefundFailed> CoinsRefundFailed { get; set; }
    
    public Event<PaymentConfirmed> PaymentConfirmed { get; private set; }
    public Event<PaymentFailed> PaymentFailed { get; private set; }
    public Event<PaymentRefunded> PaymentRefunded { get; private set; }
    public Event<PaymentRefundFailed> PaymentRefundFailed { get; private set; }
    
    public Event<OrderPaid> OrderPaid { get; private set; }
    public Event<OrderPaymentFailed> OrderPaymentFailed { get; private set; }
    
    public OrderPaymentStateMachine()
    {
        InstanceState(x => x.CurrentState);
        
        Event(() => StartOrderPaymentSaga, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPaymentSagaCompleted, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPaymentSagaFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => StockReservationExtended, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationExtensionFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationReduced, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationReductionFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => HoldCommitted, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => HoldCommitFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CoinsRefunded, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => CoinsRefundFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => PaymentConfirmed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentRefunded, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentRefundFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => OrderPaid, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPaymentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Initially(
            When(StartOrderPaymentSaga)
                .Then(context =>
                {
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.UserId = context.Message.UserId;

                    if (!context.RequestId.HasValue || context.ResponseAddress is null)
                        throw new Exception("RequestId and ResponseAddress are required");
                    
                    context.Saga.RequestId = context.RequestId.Value;
                    context.Saga.ResponseAddress = context.ResponseAddress;
                })
                .Send(new Uri("queue:stock:extend-stock-reservation"), context => new ExtendStockReservation(context.Saga.OrderId))
                .TransitionTo(AwaitingStockReservationExtension)
        );
        
        During(AwaitingStockReservationExtension,
            When(StockReservationExtended)
                .Send(new Uri("queue:wallet:commit-hold"), context => new CommitHold(context.Saga.OrderId, context.Saga.UserId))
                .TransitionTo(AwaitingCoinsHoldCommit),
            
            When(StockReservationExtensionFailed)
                .ThenAsync(async context =>
                {
                    var message = new OrderPaymentSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize()
        );
        
        During(AwaitingCoinsHoldCommit,
            When(HoldCommitted)
                .Send(new Uri("queue:payment:confirm-payment"), context => new ConfirmPayment(context.Saga.OrderId))
                .TransitionTo(AwaitingPaymentConfirmation),
            
            When(HoldCommitFailed)
                .Send(new Uri("queue:stock:reduce-stock-reservation"), context => new ReduceStockReservation(context.Saga.OrderId))
                .TransitionTo(AwaitingStockReservationReduction)
        );
        
        During(AwaitingStockReservationReduction,
            When(StockReservationReduced)
                .ThenAsync(async context =>
                {
                    var message = new OrderPaymentSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize(),
            
            When(StockReservationReductionFailed)
                .ThenAsync(async context =>
                {
                    var message = new OrderPaymentSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize()
        );
        
        During(AwaitingPaymentConfirmation,
            When(PaymentConfirmed)
                .Send(new Uri("queue:ordering:move-order-to-paid-state"), context => new MoveOrderToPaidState(context.Saga.OrderId))
                .TransitionTo(AwaitingOrderPayment),
            
            When(PaymentFailed)
                .Send(new Uri("queue:wallet:refund-coins"), context => new RefundCoins(context.Saga.OrderId))
                .TransitionTo(AwaitingCoinsRefund)
        );
        
        During(AwaitingOrderPayment,
            When(OrderPaid)
                .ThenAsync(async context =>
                {
                    var message = new OrderPaymentSagaCompleted(context.Saga.OrderId);
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize(),
            
            When(OrderPaymentFailed)
                .Send(new Uri("queue:payment:refund-payment"), context => new RefundPayment(context.Saga.OrderId))
                .TransitionTo(AwaitingPaymentRefund)
        );
        
        During(AwaitingPaymentRefund,
            When(PaymentRefunded)
                .Send(new Uri("queue:wallet:refund-coins"), context => new RefundCoins(context.Saga.OrderId))
                .TransitionTo(AwaitingCoinsRefund),
            
            When(PaymentRefundFailed)
                .Send(new Uri("queue:wallet:refund-coins"), context => new RefundCoins(context.Saga.OrderId))
                .TransitionTo(AwaitingCoinsRefund)
        );
        
        During(AwaitingCoinsRefund,
            When(CoinsRefunded)
                .ThenAsync(async context =>
                {
                    var message = new OrderPaymentSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize(),
            
            When(CoinsRefundFailed)
                .ThenAsync(async context =>
                {
                    var message = new OrderPaymentSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize()
        );
        
        SetCompletedWhenFinalized();
    }
}
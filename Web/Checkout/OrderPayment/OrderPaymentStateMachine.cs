using MassTransit;
using Web.Services.Ordering;
using Web.Services.Payment;

namespace Web.Checkout.OrderPayment;

public class OrderPaymentState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }
    
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    
    public Guid RequestId { get; set; }
    public Uri ResponseAddress { get; set; }
}

public class OrderPaymentStateMachine : MassTransitStateMachine<OrderPaymentState>
{
    public State WaitingForPaymentConfirmation { get; private set; }
    public State WaitingForOrderPayment { get; private set; }
    public State Compensating { get; private set; }
    
    public Event<ConfirmOrderPayment> ConfirmOrderPayment { get; private set; }
    public Event<OrderPaymentSagaFailed> OrderPaymentSagaFailed { get; private set; }
    public Event<OrderPaymentSagaCompleted> OrderPaymentSagaCompleted { get; private set; }
    
    public Event<PaymentConfirmed> PaymentConfirmed { get; private set; }
    public Event<PaymentFailed> PaymentFailed { get; private set; }
    public Event<PaymentRefunded> PaymentRefunded { get; private set; }
    public Event<PaymentRefundFailed> PaymentRefundFailed { get; private set; }
    
    public Event<OrderPaid> OrderPaid { get; private set; }
    public Event<OrderPaymentFailed> OrderPaymentFailed { get; private set; }
    
    public OrderPaymentStateMachine()
    {
        InstanceState(x => x.CurrentState);
        
        Event(() => ConfirmOrderPayment, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPaymentSagaFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPaymentSagaCompleted, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => PaymentConfirmed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentRefunded, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentRefundFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => OrderPaid, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderPaymentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Initially(
            When(ConfirmOrderPayment)
                .Then(context =>
                {
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.UserId = context.Message.UserId;

                    if (!context.RequestId.HasValue || context.ResponseAddress is null)
                        throw new Exception("RequestId and ResponseAddress are required");
                    
                    context.Saga.RequestId = context.RequestId.Value;
                    context.Saga.ResponseAddress = context.ResponseAddress;
                })
                .Send(new Uri("queue:confirm-payment"), context => new ConfirmPayment(context.Saga.OrderId))
                .TransitionTo(WaitingForPaymentConfirmation)
        );
        
        During(WaitingForPaymentConfirmation,
            When(PaymentConfirmed)
                .Send(new Uri("queue:move-order-to-paid-state"), context => new MoveOrderToPaidState(context.Saga.OrderId))
                .TransitionTo(WaitingForOrderPayment),
            
            When(PaymentFailed)
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
        
        During(WaitingForOrderPayment,
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
            
            When(OrderPaymentSagaFailed)
                .Send(new Uri("queue:refund-payment"), context => new RefundPayment(context.Saga.OrderId))
                .TransitionTo(Compensating)
        );
        
        During(Compensating,
            When(PaymentRefunded)
                .ThenAsync(async context =>
                {
                    var message = new OrderPaymentSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize(),
            
            When(PaymentRefundFailed)
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
    }
}
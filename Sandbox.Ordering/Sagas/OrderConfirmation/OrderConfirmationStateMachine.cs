using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Sandbox.Ordering.Services;
using Sandbox.Stock.Shared;

namespace Sandbox.Ordering.Sagas.OrderConfirmation;

public class OrderConfirmationState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public uint RowVersion { get; set; }
    public string CurrentState { get; set; }
    
    public Guid OrderId { get; set; }
    
    public Guid RequestId { get; set; }
    public Uri ResponseAddress { get; set; }
}

public class OrderConfirmationStateMachine : MassTransitStateMachine<OrderConfirmationState>
{
    public State AwaitingStockReservationConfirmation { get; private set; }
    public State AwaitingStockReservationReversion { get; private set; }
    
    public Event<StartOrderConfirmationSaga> StartOrderConfirmationSaga { get; private set; }
    public Event<OrderConfirmationSagaFailed> OrderConfirmationSagaFailed { get; private set; }
    public Event<OrderConfirmationSagaCompleted> OrderConfirmationSagaCompleted { get; private set; }
    
    public Event<StockReservationConfirmed> StockReservationConfirmed { get; private set; }
    public Event<StockReservationConfirmationFailed>  StockReservationConfirmationFailed { get; private set; }
    public Event<StockReservationReverted> StockReservationReverted { get; private set; }
    public Event<StockReservationReversionFailed> StockReservationReversionFailed { get; private set; }
    
    public OrderConfirmationStateMachine()
    {
        InstanceState(x => x.CurrentState);
        
        Event(() => StartOrderConfirmationSaga, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderConfirmationSagaFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderConfirmationSagaCompleted, x => x.CorrelateById(context => context.Message.OrderId));
        
        Event(() => StockReservationConfirmed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationConfirmationFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationReverted, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationReversionFailed, x => x.CorrelateById(context => context.Message.OrderId));
        
        Initially(
            When(StartOrderConfirmationSaga)
                .Then(context =>
                {
                    context.Saga.OrderId = context.Message.OrderId;

                    if (!context.RequestId.HasValue || context.ResponseAddress is null)
                        throw new Exception("RequestId and ResponseAddress are required");
                    
                    context.Saga.RequestId = context.RequestId.Value;
                    context.Saga.ResponseAddress = context.ResponseAddress;
                })
                .Send(new Uri("queue:stock:confirm-stock-reservation"), context => new ConfirmStockReservation(context.Saga.OrderId))
                .TransitionTo(AwaitingStockReservationConfirmation)
        );
        
        During(AwaitingStockReservationConfirmation,
            When(StockReservationConfirmed)
                .ThenAsync(async context =>
                {
                    var orderService = context.GetPayload<IServiceProvider>().GetRequiredService<IOrderService>();
                    var command = new ConfirmOrderCommand(context.Saga.OrderId);
                    await orderService.ConfirmOrderAsync(command, context.CancellationToken);
                    
                    var message = new OrderConfirmationSagaCompleted(context.Saga.OrderId);
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize()
                .Catch<Exception>(exception => exception
                    .Send(new Uri("queue:stock:revert-stock-reservation"), context => new RevertStockReservation(context.Saga.OrderId))
                    .TransitionTo(AwaitingStockReservationReversion)
                ),
            
            When(StockReservationConfirmationFailed)
                .ThenAsync(async context =>
                {
                    var message = new OrderConfirmationSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize()
        );
        
        During(AwaitingStockReservationReversion,
            When(StockReservationReverted)
                .ThenAsync(async context =>
                {
                    var message = new OrderConfirmationSagaFailed(context.Saga.OrderId, "");
                    await context.Send(context.Saga.ResponseAddress, message, sendContext =>
                    {
                        sendContext.RequestId = context.Saga.RequestId;
                    });
                })
                .Finalize(),
            
            When(StockReservationReversionFailed)
                .ThenAsync(async context =>
                {
                    var message = new OrderConfirmationSagaFailed(context.Saga.OrderId, "");
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
using MassTransit;

namespace Web.Services.Ordering;

public class PayOrderConsumer : IConsumer<PayOrder>
{
    public async Task Consume(ConsumeContext<PayOrder> context)
    {
        Console.WriteLine("Order has been paid");
        await context.Publish(new OrderPaid(context.Message.OrderId));
        // await context.Publish(new OrderPaymentFailed(context.Message.OrderId, ""));
    }
}
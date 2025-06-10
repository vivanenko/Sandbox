using MassTransit;

namespace Web.Services.Ordering;

public class PlaceOrderConsumer : IConsumer<PlaceOrder>
{
    public async Task Consume(ConsumeContext<PlaceOrder> context)
    {
        Console.WriteLine("Order has been placed");
        await context.Publish(new OrderPlaced(context.Message.OrderId));
        // await context.Publish(new OrderPlacementFailed(context.Message.OrderId, ""));
    }
}
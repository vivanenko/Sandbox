using MassTransit;
using Sandbox.Ordering.Shared;

namespace Sandbox.Ordering;

public class PlaceOrderConsumer : IConsumer<PlaceOrder>
{
    public async Task Consume(ConsumeContext<PlaceOrder> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Order has been placed");
            Console.ResetColor();
            await context.Publish(new OrderPlaced(context.Message.OrderId));            
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Order placement failed");
            Console.ResetColor();
            await context.Publish(new OrderPlacementFailed(context.Message.OrderId, ""));
        }
    }
}
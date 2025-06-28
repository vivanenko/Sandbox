using MassTransit;
using Sandbox.Ordering.Shared;

namespace Sandbox.Ordering;

public class MoveOrderToPaidStateConsumer : IConsumer<MoveOrderToPaidState>
{
    public async Task Consume(ConsumeContext<MoveOrderToPaidState> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Order has been moved to paid state");
            Console.ResetColor();
            await context.Publish(new OrderPaid(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Order has not been moved to paid state");
            Console.ResetColor();
            await context.Publish(new OrderPaymentFailed(context.Message.OrderId, ""));
        }
    }
}
using MassTransit;
using Sandbox.Ordering.Shared;

namespace Sandbox.Ordering;

public class ConfirmOrderConsumer : IConsumer<ConfirmOrder>
{
    public async Task Consume(ConsumeContext<ConfirmOrder> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Order has been confirmed");
            Console.ResetColor();
            await context.Publish(new OrderConfirmed(context.Message.OrderId));            
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Order confirmation failed");
            Console.ResetColor();
            await context.Publish(new OrderConfirmationFailed(context.Message.OrderId, ""));
        }
    }
}
using MassTransit;

namespace Web.Services.Wallet;

public class CancelHoldConsumer : IConsumer<CancelHold>
{
    public async Task Consume(ConsumeContext<CancelHold> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Hold has been cancelled");
            Console.ResetColor();
            await context.Publish(new HoldCancelled(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Hold cancellation failed");
            Console.ResetColor();
            await context.Publish(new HoldCancellationFailed(context.Message.OrderId, ""));
        }
    }
}
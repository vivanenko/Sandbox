using MassTransit;
using Sandbox.Wallet.Shared;

namespace Sandbox.Wallet;

public class HoldCoinsConsumer : IConsumer<HoldCoins>
{
    public async Task Consume(ConsumeContext<HoldCoins> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Coins have been held");
            Console.ResetColor();
            await context.Publish(new CoinsHeld(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Coins hold failed");
            Console.ResetColor();
            await context.Publish(new CoinsHoldFailed(context.Message.OrderId, ""));   
        }
    }
}
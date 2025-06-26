using MassTransit;

namespace Web.Services.Wallet;

public class RefundCoinsConsumer : IConsumer<RefundCoins>
{
    public async Task Consume(ConsumeContext<RefundCoins> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Coins have been refunded");
            Console.ResetColor();
            await context.Publish(new CoinsRefunded(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Coins refund failed");
            Console.ResetColor();
            await context.Publish(new CoinsRefundFailed(context.Message.OrderId, ""));   
        }
    }
}
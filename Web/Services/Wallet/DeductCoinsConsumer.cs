using MassTransit;

namespace Web.Services.Wallet;

public class DeductCoinsConsumer : IConsumer<DeductCoins>
{
    public async Task Consume(ConsumeContext<DeductCoins> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Coins have been deducted");
            Console.ResetColor();
            await context.Publish(new CoinsDeducted(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Coins deduction failed");
            Console.ResetColor();
            await context.Publish(new CoinsDeductionFailed(context.Message.OrderId, ""));   
        }
    }
}
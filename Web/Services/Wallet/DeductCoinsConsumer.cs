using MassTransit;

namespace Web.Services.Wallet;

public class DeductCoinsConsumer : IConsumer<DeductCoins>
{
    public async Task Consume(ConsumeContext<DeductCoins> context)
    {
        Console.WriteLine("Coins have been deducted");
        await context.Publish(new CoinsDeducted(context.Message.OrderId));
        // await context.Publish(new CoinsDeductionFailed(context.Message.OrderId, ""));
    }
}
using MassTransit;

namespace Web.Services.Wallet;

public class RefundCoinsConsumer : IConsumer<RefundCoins>
{
    public async Task Consume(ConsumeContext<RefundCoins> context)
    {
        Console.WriteLine("Coins have been refunded");
        await context.Publish(new CoinsRefunded(context.Message.OrderId));
    }
}
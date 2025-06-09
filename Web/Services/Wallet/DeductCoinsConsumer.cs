using MassTransit;
using Web.Saga;

namespace Web.Services.Wallet;

public class DeductCoinsConsumer : IConsumer<DeductCoins>
{
    public async Task Consume(ConsumeContext<DeductCoins> context)
    {
        await context.Publish(new CoinsDeducted(context.Message.OrderId));
        // await context.Publish(new CoinsDeductionFailed(context.Message.OrderId, ""));
    }
}
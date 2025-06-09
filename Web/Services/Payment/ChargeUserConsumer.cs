using MassTransit;

namespace Web.Services.Payment;

public class ChargeUserConsumer : IConsumer<ChargeUser>
{
    public async Task Consume(ConsumeContext<ChargeUser> context)
    {
        await context.Publish(new PaymentCharged(context.Message.OrderId));
        // await context.Publish(new PaymentFailed(context.Message.OrderId, ""));
    }
}
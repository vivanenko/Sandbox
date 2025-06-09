using MassTransit;

namespace Web.Services.Payment;

public class RefundPaymentConsumer : IConsumer<RefundPayment>
{
    public async Task Consume(ConsumeContext<RefundPayment> context)
    {
        await context.Publish(new PaymentRefunded(context.Message.OrderId));
    }
}
using MassTransit;

namespace Web.Services.Payment;

public class RefundPaymentConsumer : IConsumer<RefundPayment>
{
    public async Task Consume(ConsumeContext<RefundPayment> context)
    {
        Console.WriteLine("Payment has been refunded");
        await context.Publish(new PaymentRefunded(context.Message.OrderId));
    }
}
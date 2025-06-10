using MassTransit;

namespace Web.Services.Payment;

public class ConfirmPaymentConsumer : IConsumer<ConfirmPayment>
{
    public async Task Consume(ConsumeContext<ConfirmPayment> context)
    {
        Console.WriteLine("Payment confirmation");
        await context.Publish(new PaymentConfirmed(context.Message.OrderId));
        // await context.Publish(new PaymentFailed(context.Message.OrderId, ""));
    }
}
using MassTransit;

namespace Web.Services.Payment;

public class CancelPaymentIntentConsumer : IConsumer<CancelPaymentIntent>
{
    public async Task Consume(ConsumeContext<CancelPaymentIntent> context)
    {
        Console.WriteLine("Payment has been cancelled");
        await context.Publish(new PaymentIntentCancelled(context.Message.OrderId));
    }
}
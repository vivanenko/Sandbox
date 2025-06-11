using MassTransit;

namespace Web.Services.Payment;

public class CancelPaymentIntentConsumer : IConsumer<CancelPaymentIntent>
{
    public async Task Consume(ConsumeContext<CancelPaymentIntent> context)
    {
        Console.WriteLine("Cancelling payment intent");
        await context.Publish(new PaymentIntentCancelled(context.Message.OrderId));
        // await context.Publish(new PaymentIntentCancellationFailed(context.Message.OrderId, ""));
    }
}
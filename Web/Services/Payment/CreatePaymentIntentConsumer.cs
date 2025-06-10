using MassTransit;

namespace Web.Services.Payment;

public class CreatePaymentIntentConsumer : IConsumer<CreatePaymentIntent>
{
    public async Task Consume(ConsumeContext<CreatePaymentIntent> context)
    {
        Console.WriteLine("Payment intent has been created");
        await context.Publish(new PaymentIntentCreated(context.Message.OrderId));
        // await context.Publish(new PaymentIntentFailed(context.Message.OrderId, ""));
    }
}
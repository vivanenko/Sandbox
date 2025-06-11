using MassTransit;

namespace Web.Services.Payment;

public class CreatePaymentIntentConsumer : IConsumer<CreatePaymentIntent>
{
    public async Task Consume(ConsumeContext<CreatePaymentIntent> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Payment intent has been created");
            Console.ResetColor();
            await context.Publish(new PaymentIntentCreated(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Payment intent failed");
            Console.ResetColor();
            await context.Publish(new PaymentIntentFailed(context.Message.OrderId, ""));   
        }
    }
}
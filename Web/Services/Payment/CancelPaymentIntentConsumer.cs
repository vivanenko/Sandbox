using MassTransit;

namespace Web.Services.Payment;

public class CancelPaymentIntentConsumer : IConsumer<CancelPaymentIntent>
{
    public async Task Consume(ConsumeContext<CancelPaymentIntent> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Payment intent has been cancelled");
            Console.ResetColor();
            await context.Publish(new PaymentIntentCancelled(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Payment intent cancellation failed");
            Console.ResetColor();
            await context.Publish(new PaymentIntentCancellationFailed(context.Message.OrderId, ""));
        }
    }
}
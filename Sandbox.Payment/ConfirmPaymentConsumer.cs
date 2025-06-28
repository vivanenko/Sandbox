using MassTransit;
using Sandbox.Payment.Shared;

namespace Sandbox.Payment;

public class ConfirmPaymentConsumer : IConsumer<ConfirmPayment>
{
    public async Task Consume(ConsumeContext<ConfirmPayment> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Payment has been confirmed");
            Console.ResetColor();
            await context.Publish(new PaymentConfirmed(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Payment confirmation failed");
            Console.ResetColor();
            await context.Publish(new PaymentFailed(context.Message.OrderId, ""));
        }
    }
}
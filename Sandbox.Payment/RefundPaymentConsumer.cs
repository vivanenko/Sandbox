using MassTransit;
using Sandbox.Payment.Shared;

namespace Sandbox.Payment;

public class RefundPaymentConsumer : IConsumer<RefundPayment>
{
    public async Task Consume(ConsumeContext<RefundPayment> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Payment has been refunded");
            Console.ResetColor();
            await context.Publish(new PaymentRefunded(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Payment refund failed");
            Console.ResetColor();
            await context.Publish(new PaymentRefundFailed(context.Message.OrderId, ""));
        }
    }
}
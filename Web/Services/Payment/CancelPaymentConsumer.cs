using MassTransit;
using Web.Saga;

namespace Web.Services.Payment;

public class CancelPaymentConsumer : IConsumer<CancelPayment>
{
    public async Task Consume(ConsumeContext<CancelPayment> context)
    {
        Console.WriteLine("Payment has been cancelled");
        await context.Publish(new PaymentCancelled(context.Message.OrderId));
    }
}
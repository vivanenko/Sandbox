using MassTransit;
using Sandbox.Stock.Shared;

namespace Sandbox.Stock;

public class ConfirmStockReservationConsumer : IConsumer<ConfirmStockReservation>
{
    public async Task Consume(ConsumeContext<ConfirmStockReservation> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Stock reservation has been confirmed");
            Console.ResetColor();
            await context.Publish(new StockReservationConfirmed(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stock reservation confirmation failed");
            Console.ResetColor();
            await context.Publish(new StockReservationConfirmationFailed(context.Message.OrderId, ""));
        }
    }
}
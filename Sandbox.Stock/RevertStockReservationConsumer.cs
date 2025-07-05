using MassTransit;
using Sandbox.Stock.Shared;

namespace Sandbox.Stock;

public class RevertStockReservationConsumer : IConsumer<RevertStockReservation>
{
    public async Task Consume(ConsumeContext<RevertStockReservation> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Stock reservation has been reverted");
            Console.ResetColor();
            await context.Publish(new StockReservationReverted(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stock reservation reversion failed");
            Console.ResetColor();
            await context.Publish(new StockReservationReversionFailed(context.Message.OrderId, ""));
        }
    }
}
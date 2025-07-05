using MassTransit;
using Sandbox.Stock.Shared;

namespace Sandbox.Stock;

public class ExtendStockReservationConsumer : IConsumer<ExtendStockReservation>
{
    public async Task Consume(ConsumeContext<ExtendStockReservation> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Stock reservation has been extended");
            Console.ResetColor();
            await context.Publish(new StockReservationExtended(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stock reservation extension failed");
            Console.ResetColor();
            await context.Publish(new StockReservationExtensionFailed(context.Message.OrderId, ""));
        }
    }
}
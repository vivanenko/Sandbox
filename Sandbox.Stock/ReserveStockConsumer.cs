using MassTransit;
using Sandbox.Stock.Shared;

namespace Sandbox.Stock;

public class ReserveStockConsumer : IConsumer<ReserveStock>
{
    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Stock has been reserved");
            Console.ResetColor();
            await context.Publish(new StockReserved(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stock reservation failed");
            Console.ResetColor();
            await context.Publish(new StockReservationFailed(context.Message.OrderId, ""));   
        }
    }
}
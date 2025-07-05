using MassTransit;
using Sandbox.Stock.Shared;

namespace Sandbox.Stock;

public class ReduceStockReservationConsumer : IConsumer<ReduceStockReservation>
{
    public async Task Consume(ConsumeContext<ReduceStockReservation> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Stock reservation has been reduced");
            Console.ResetColor();
            await context.Publish(new StockReservationReduced(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stock reservation reduction failed");
            Console.ResetColor();
            await context.Publish(new StockReservationReductionFailed(context.Message.OrderId, ""));
        }
    }
}
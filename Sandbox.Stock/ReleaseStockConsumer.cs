using MassTransit;
using Sandbox.Stock.Shared;

namespace Sandbox.Stock;

public class ReleaseStockConsumer : IConsumer<ReleaseStock>
{
    public async Task Consume(ConsumeContext<ReleaseStock> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Stock has been released");
            Console.ResetColor();
            await context.Publish(new StockReleased(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stock release failed");
            Console.ResetColor();
            await context.Publish(new StockReleaseFailed(context.Message.OrderId, ""));
        }
    }
}
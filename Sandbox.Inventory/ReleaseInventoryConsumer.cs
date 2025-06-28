using MassTransit;
using Sandbox.Inventory.Shared;

namespace Sandbox.Inventory;

public class ReleaseInventoryConsumer : IConsumer<ReleaseInventory>
{
    public async Task Consume(ConsumeContext<ReleaseInventory> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Inventory has been released");
            Console.ResetColor();
            await context.Publish(new InventoryReleased(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Inventory release failed");
            Console.ResetColor();
            await context.Publish(new InventoryReleaseFailed(context.Message.OrderId, ""));
        }
    }
}
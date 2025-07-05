using MassTransit;
using Sandbox.Inventory.Shared;

namespace Sandbox.Inventory;

public class ExtendInventoryReservationConsumer : IConsumer<ExtendInventoryReservation>
{
    public async Task Consume(ConsumeContext<ExtendInventoryReservation> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Inventory reservation has been extended");
            Console.ResetColor();
            await context.Publish(new InventoryReservationExtended(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Inventory reservation extension failed");
            Console.ResetColor();
            await context.Publish(new InventoryReservationExtensionFailed(context.Message.OrderId, ""));
        }
    }
}
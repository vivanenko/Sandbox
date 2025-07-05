using MassTransit;
using Sandbox.Inventory.Shared;

namespace Sandbox.Inventory;

public class ReduceInventoryReservationConsumer : IConsumer<ReduceInventoryReservation>
{
    public async Task Consume(ConsumeContext<ReduceInventoryReservation> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Inventory reservation has been reduced");
            Console.ResetColor();
            await context.Publish(new InventoryReservationReduced(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Inventory reservation reduction failed");
            Console.ResetColor();
            await context.Publish(new InventoryReservationReductionFailed(context.Message.OrderId, ""));
        }
    }
}
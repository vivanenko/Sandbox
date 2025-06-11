using MassTransit;

namespace Web.Services.Inventory;

public class ReserveInventoryConsumer : IConsumer<ReserveInventory>
{
    public async Task Consume(ConsumeContext<ReserveInventory> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Inventory has been reserved");
            Console.ResetColor();
            await context.Publish(new InventoryReserved(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Inventory reservation failed");
            Console.ResetColor();
            await context.Publish(new InventoryReservationFailed(context.Message.OrderId, ""));   
        }
    }
}
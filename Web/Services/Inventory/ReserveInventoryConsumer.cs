using MassTransit;

namespace Web.Services.Inventory;

public class ReserveInventoryConsumer : IConsumer<ReserveInventory>
{
    public async Task Consume(ConsumeContext<ReserveInventory> context)
    {
        Console.WriteLine("Inventory has been reserved");
        await context.Publish(new InventoryReserved(context.Message.OrderId));
        // await context.Publish(new InventoryReservationFailed(context.Message.OrderId, ""));
    }
}
using MassTransit;
using Web.Saga;

namespace Web.Services.Inventory;

public class ReserveInventoryConsumer : IConsumer<ReserveInventory>
{
    public async Task Consume(ConsumeContext<ReserveInventory> context)
    {
        await context.Publish(new InventoryReserved(context.Message.OrderId));
        // await context.Publish(new InventoryReservationFailed(context.Message.OrderId, ""));
    }
}
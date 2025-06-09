using MassTransit;
using Web.Saga;

namespace Web.Services.Inventory;

public class CancelReservationConsumer : IConsumer<CancelReservation>
{
    public async Task Consume(ConsumeContext<CancelReservation> context)
    {
        Console.WriteLine("Reservation has been cancelled");
        await context.Publish(new InventoryReservationCancelled(context.Message.OrderId));
    }
}
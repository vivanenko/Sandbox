using MassTransit;

namespace Web.Services.Inventory;

public class CancelReservationConsumer : IConsumer<CancelReservation>
{
    public async Task Consume(ConsumeContext<CancelReservation> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Reservation has been cancelled");
            Console.ResetColor();
            await context.Publish(new InventoryReservationCancelled(context.Message.OrderId));
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
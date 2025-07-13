using MassTransit;
using Sandbox.Ordering.Shared;

namespace Sandbox.Ordering.Services;

public record PlaceOrderCommand(Guid Id);
public record PlaceOrderResponse(Guid Id, bool AlreadyExists);

public record PayForOrderCommand(Guid Id);
public record PayForOrderResponse(Guid Id);

public record ConfirmOrderCommand(Guid Id);
public record ConfirmOrderResponse(Guid Id);

public interface IOrderService
{
    Task<PlaceOrderResponse> TryPlaceOrderAsync(PlaceOrderCommand command, CancellationToken cancellationToken);
    Task<PayForOrderResponse> PayForOrderAsync(PayForOrderCommand command, CancellationToken cancellationToken);
    Task<ConfirmOrderResponse> ConfirmOrderAsync(ConfirmOrderCommand command, CancellationToken cancellationToken);
}

public class OrderService(IPublishEndpoint publishEndpoint) : IOrderService
{
    public async Task<PlaceOrderResponse> TryPlaceOrderAsync(PlaceOrderCommand command, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Saving order to the database in Placed state");
        var success = true;
        if (!success) throw new Exception("Failed to save order to the database");
        
        await publishEndpoint.Publish(new OrderPlaced(command.Id), cancellationToken);
        return new PlaceOrderResponse(command.Id, false);
    }

    public async Task<PayForOrderResponse> PayForOrderAsync(PayForOrderCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine("Saving order to the database in Paid state");
        var success = true;
        if (!success) throw new Exception("Failed to save order to the database");
        
        await publishEndpoint.Publish(new OrderPaid(command.Id), cancellationToken);
        return new PayForOrderResponse(command.Id);
    }

    public async Task<ConfirmOrderResponse> ConfirmOrderAsync(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine("Saving order to the database in Confirmed state");
        var success = true;
        if (!success) throw new Exception("Failed to save order to the database");
        
        await publishEndpoint.Publish(new OrderConfirmed(command.Id), cancellationToken);
        return new ConfirmOrderResponse(command.Id);
    }
}
using Web.Common;

namespace Web.Checkout.OrderPlacement;

public record StartOrderPlacement(Guid OrderId, Guid UserId, decimal Amount, int CoinsAmount, List<OrderItemDto> Items);
public record OrderPlacementCompleted(Guid OrderId, bool IsPaymentConfirmationRequired);
public record OrderPlacementFailed(Guid OrderId, string Reason);
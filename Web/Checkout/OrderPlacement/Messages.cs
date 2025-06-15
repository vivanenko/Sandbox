using Web.Common;

namespace Web.Checkout.OrderPlacement;

public record StartOrderPlacement(Guid OrderId, Guid UserId, decimal Amount, int CoinsAmount, List<OrderItemDto> Items);
public record CheckoutCompleted(Guid OrderId, bool IsPaymentConfirmationRequired);
public record CheckoutFailed(Guid OrderId, string Reason);
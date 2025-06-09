using Web.Common;

namespace Web.Checkout;

public record StartCheckout(Guid OrderId, Guid UserId, decimal Amount, int CoinsAmount, List<OrderItemDto> Items);
public record CheckoutSucceeded(Guid OrderId);
public record CheckoutFailed(Guid OrderId, string Reason);
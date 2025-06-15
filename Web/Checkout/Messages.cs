using Web.Common;

namespace Web.Checkout;

public record StartCheckout(Guid OrderId, Guid UserId, decimal Amount, int CoinsAmount, List<OrderItemDto> Items);
public record CheckoutConfirmationRequested(Guid OrderId);

public record ConfirmCheckout(Guid OrderId);
public record CheckoutCompleted(Guid OrderId);
public record CheckoutFailed(Guid OrderId, string Reason);
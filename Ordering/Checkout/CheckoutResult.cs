namespace Ordering.Checkout;

public record CheckoutResult(Guid OrderId, bool IsPaid, Invoice? Invoice);
public record Invoice(Guid Id, string RedirectUrl);
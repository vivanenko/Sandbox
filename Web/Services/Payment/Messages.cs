namespace Web.Services.Payment;

public record CreatePaymentIntent(Guid OrderId, Guid UserId, decimal Amount);
public record PaymentIntentCreated(Guid OrderId);
public record PaymentIntentFailed(Guid OrderId, string Reason);
public record CancelPaymentIntent(Guid OrderId);
public record PaymentIntentCancelled(Guid OrderId);
namespace Web.Services.Payment;

public record CreatePaymentIntent(Guid OrderId, Guid UserId, decimal Amount);
public record PaymentIntentCreated(Guid OrderId);
public record PaymentIntentFailed(Guid OrderId, string Reason);
public record CancelPaymentIntent(Guid OrderId);
public record PaymentIntentCancelled(Guid OrderId);
public record PaymentIntentCancellationFailed(Guid OrderId, string Reason);

public record ConfirmPayment(Guid OrderId);
public record PaymentConfirmed(Guid OrderId);
public record PaymentFailed(Guid OrderId, string Reason);
public record RefundPayment(Guid OrderId);
public record PaymentRefunded(Guid OrderId);
public record PaymentRefundFailed(Guid OrderId, string Reason);
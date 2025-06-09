namespace Web.Services.Payment;

public record ChargeUser(Guid OrderId, Guid UserId, decimal Amount);
public record PaymentCharged(Guid OrderId);
public record PaymentFailed(Guid OrderId, string Reason);
public record CancelPayment(Guid OrderId);
public record PaymentCancelled(Guid OrderId);
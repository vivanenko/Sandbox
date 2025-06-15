namespace Web.Checkout.OrderPayment;

public record ConfirmOrderPayment(Guid OrderId, Guid UserId);
public record OrderPaymentSagaFailed(Guid OrderId, string Reason);
public record OrderPaymentSagaCompleted(Guid OrderId);
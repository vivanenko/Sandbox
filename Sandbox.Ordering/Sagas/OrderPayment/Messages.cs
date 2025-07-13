namespace Sandbox.Ordering.Sagas.OrderPayment;

public record StartOrderPaymentSaga(Guid OrderId, Guid? HoldId);
public record OrderPaymentSagaFailed(Guid OrderId, string Reason);
public record OrderPaymentSagaCompleted(Guid OrderId);
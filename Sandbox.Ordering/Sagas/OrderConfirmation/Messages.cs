namespace Sandbox.Ordering.Sagas.OrderConfirmation;

public record StartOrderConfirmationSaga(Guid OrderId);
public record OrderConfirmationSagaFailed(Guid OrderId, string Reason);
public record OrderConfirmationSagaCompleted(Guid OrderId);
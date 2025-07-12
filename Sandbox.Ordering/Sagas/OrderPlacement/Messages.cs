using Sandbox.Ordering.Models;

namespace Sandbox.Ordering.Sagas.OrderPlacement;

public record StartOrderPlacementSaga(Guid OrderId, Guid UserId, decimal Amount, int CoinsAmount, StockItem[] Items);
public record OrderPlacementSagaCompleted(Guid OrderId, bool IsPaymentConfirmationRequired);
public record OrderPlacementSagaFailed(Guid OrderId, string Reason);
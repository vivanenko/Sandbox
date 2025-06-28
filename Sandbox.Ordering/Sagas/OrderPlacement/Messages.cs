using Sandbox.Inventory.Shared;

namespace Sandbox.Ordering.Sagas.OrderPlacement;

public record StartOrderPlacementSaga(Guid OrderId, Guid UserId, decimal Amount, int CoinsAmount, List<ItemDto> Items);
public record OrderPlacementSagaCompleted(Guid OrderId, bool IsPaymentConfirmationRequired);
public record OrderPlacementSagaFailed(Guid OrderId, string Reason);
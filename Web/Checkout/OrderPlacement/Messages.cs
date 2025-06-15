using Web.Common;

namespace Web.Checkout.OrderPlacement;

public record StartOrderPlacementSaga(Guid OrderId, Guid UserId, decimal Amount, int CoinsAmount, List<OrderItemDto> Items);
public record OrderPlacementSagaCompleted(Guid OrderId, bool IsPaymentConfirmationRequired);
public record OrderPlacementSagaFailed(Guid OrderId, string Reason);
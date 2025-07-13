namespace Sandbox.Ordering.Shared;

public record OrderPlaced(Guid OrderId);
public record OrderPlacementFailed(Guid OrderId, string Reason);

public record OrderPaid(Guid OrderId);
public record OrderPaymentFailed(Guid OrderId, string Reason);

public record ConfirmOrder(Guid OrderId);
public record OrderConfirmed(Guid OrderId);
public record OrderConfirmationFailed(Guid OrderId, string Reason);
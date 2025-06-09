namespace Web.Services.Ordering;

public record PlaceOrder(Guid OrderId);
public record OrderPlaced(Guid OrderId);
public record OrderPlacementFailed(Guid OrderId, string Reason);

public record PayOrder(Guid OrderId);
public record OrderPaid(Guid OrderId);
public record OrderPaymentFailed(Guid OrderId, string Reason);
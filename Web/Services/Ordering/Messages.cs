namespace Web.Services.Ordering;

public record PlaceOrder(Guid OrderId);
public record OrderPlaced(Guid OrderId);
public record OrderPlacementFailed(Guid OrderId, string Reason);
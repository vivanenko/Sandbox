using Web.Common;

namespace Web.Services.Inventory;

public record ReserveInventory(Guid OrderId, List<OrderItemDto> Items);
public record InventoryReserved(Guid OrderId);
public record InventoryReservationFailed(Guid OrderId, string Reason);
public record CancelReservation(Guid OrderId);
public record InventoryReservationCancelled(Guid OrderId);
public record InventoryReleaseFailed(Guid OrderId, string Reason);
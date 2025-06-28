using Web.Common;

namespace Web.Services.Inventory;

public record ReserveInventory(Guid OrderId, List<OrderItemDto> Items);
public record InventoryReserved(Guid OrderId);
public record InventoryReservationFailed(Guid OrderId, string Reason);
public record ReleaseInventory(Guid OrderId);
public record InventoryReleased(Guid OrderId);
public record InventoryReleaseFailed(Guid OrderId, string Reason);
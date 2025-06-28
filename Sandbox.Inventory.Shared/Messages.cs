namespace Sandbox.Inventory.Shared;

public record ReserveInventory(Guid OrderId, List<ItemDto> Items);
public record InventoryReserved(Guid OrderId);
public record InventoryReservationFailed(Guid OrderId, string Reason);
public record ReleaseInventory(Guid OrderId);
public record InventoryReleased(Guid OrderId);
public record InventoryReleaseFailed(Guid OrderId, string Reason);
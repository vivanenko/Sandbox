namespace Sandbox.Stock.Shared;

public record ReserveInventory(Guid OrderId, ItemDto[] Items);
public record InventoryReserved(Guid OrderId);
public record InventoryReservationFailed(Guid OrderId, string Reason);

public record ReleaseInventory(Guid OrderId);
public record InventoryReleased(Guid OrderId);
public record InventoryReleaseFailed(Guid OrderId, string Reason);

public record ExtendInventoryReservation(Guid OrderId);
public record InventoryReservationExtended(Guid OrderId);
public record InventoryReservationExtensionFailed(Guid OrderId, string Reason);

public record ReduceInventoryReservation(Guid OrderId);
public record InventoryReservationReduced(Guid OrderId);
public record InventoryReservationReductionFailed(Guid OrderId, string Reason);
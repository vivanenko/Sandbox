namespace Sandbox.Stock.Shared;

public record ReserveStock(Guid OrderId, ItemDto[] Items);
public record StockReserved(Guid OrderId);
public record StockReservationFailed(Guid OrderId, string Reason);

public record ReleaseStock(Guid OrderId);
public record StockReleased(Guid OrderId);
public record StockReleaseFailed(Guid OrderId, string Reason);

public record ExtendStockReservation(Guid OrderId);
public record StockReservationExtended(Guid OrderId);
public record StockReservationExtensionFailed(Guid OrderId, string Reason);

public record ReduceStockReservation(Guid OrderId);
public record StockReservationReduced(Guid OrderId);
public record StockReservationReductionFailed(Guid OrderId, string Reason);
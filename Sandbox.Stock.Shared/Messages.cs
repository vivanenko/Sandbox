namespace Sandbox.Stock.Shared;

public record ReserveStock(Guid OrderId, Item[] Items);
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

public record ConfirmStockReservation(Guid OrderId);
public record StockReservationConfirmed(Guid OrderId);
public record StockReservationConfirmationFailed(Guid OrderId, string Reason);

public record RevertStockReservation(Guid OrderId);
public record StockReservationReverted(Guid OrderId);
public record StockReservationReversionFailed(Guid OrderId, string Reason);
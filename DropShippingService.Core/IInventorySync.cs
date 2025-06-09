namespace DropShippingService.Core;

public interface IInventorySync
{
    Task<IReadOnlyCollection<ProductQuantity>> GetQuantityChangesAsync(CancellationToken cancellationToken = default);
}
namespace DropShippingService.Core.Orderchamp;

[SupplierService("orderchamp")]
public class OrderchampInventorySync : IInventorySync
{
    public async Task<IReadOnlyCollection<ProductQuantity>> GetQuantityChangesAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
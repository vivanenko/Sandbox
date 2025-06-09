namespace DropShippingService.Core.Orderchamp;

[SupplierService("orderchamp")]
public class OrderchampProductSync : IProductSync
{
    public IAsyncEnumerable<Product> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
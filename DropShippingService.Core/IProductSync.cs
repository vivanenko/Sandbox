namespace DropShippingService.Core;

public interface IProductSync
{
    IAsyncEnumerable<Product> GetAllProductsAsync(CancellationToken cancellationToken = default);
}
namespace DropShippingService.Core.Orderchamp;

[SupplierService("orderchamp")]
public class OrderchampShippingProbe : IShippingProbe
{
    public async Task<ProductStatus> GetStatusAsync(ProbeRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
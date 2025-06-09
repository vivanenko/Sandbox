namespace DropShippingService.Core;

public interface IShippingProbe
{
    Task<ProductStatus> GetStatusAsync(ProbeRequest request, CancellationToken cancellationToken = default);
}
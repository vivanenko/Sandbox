using System.Net.Http.Json;

namespace Sandbox.Ordering.Clients.Cart;

public interface ICartClient
{
    Task<Cart> GetCartAsync(Guid cartId, CancellationToken cancellationToken);
}

public class CartClient(HttpClient httpClient) : ICartClient
{
    public async Task<Cart> GetCartAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await httpClient.GetFromJsonAsync<Cart>("cart", cancellationToken);
        if (cart is null) throw new Exception("Cart not found");
        return cart;
    }
}

public class Cart
{
    public CartItem[] CartItems { get; set; } = [];
}

public class CartItem
{
    public Guid Id { get; set; }
}
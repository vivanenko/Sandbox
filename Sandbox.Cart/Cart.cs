namespace Sandbox.Cart;

public class Cart
{
    public CartItem[] CartItems { get; set; } = [];
}

public class CartItem
{
    public Guid Id { get; set; }
}
namespace Ordering.Checkout;

public class CheckoutService
{
    public async Task<CheckoutResult> ProceedAsync(CreateOrder createOrder,
        CancellationToken cancellationToken = default)
    {
        // Reserve items
        // Wallet (Withdraw coins/Use vouchers)
        var paymentRequired = true; // Amount > 0
        if (paymentRequired)
        {
            // Payment: Create Invoice and add it to the order
        }
        // Clear the cart
        // Create order and OrderPlaced event (outbox)
        if (!paymentRequired)
        {
            // Mark order as Paid and create an OrderPaid event (outbox)
        }
        // Return
        throw new NotImplementedException();
    }

    public async Task ConfirmPaymentAsync(PaymentConfirmation paymentConfirmation,
        CancellationToken cancellationToken = default)
    {
        // Payment: Confirm
        // Mark order as Paid and create an OrderPaid event (outbox)
        // Return
    }
}
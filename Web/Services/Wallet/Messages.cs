namespace Web.Services.Wallet;

public record DeductCoins(Guid OrderId, Guid UserId, int Points);
public record CoinsDeducted(Guid OrderId);
public record CoinsDeductionFailed(Guid OrderId, string Reason);
public record RefundCoins(Guid OrderId, Guid UserId, int Points);
public record CoinsRefunded(Guid OrderId);
public record CoinsRefundFailed(Guid OrderId, string Reason);
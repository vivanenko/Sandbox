namespace Web.Services.Wallet;

public record HoldCoins(Guid OrderId, Guid UserId, int Points);
public record CoinsHeld(Guid OrderId);
public record CoinsHoldFailed(Guid OrderId, string Reason);
public record CancelHold(Guid OrderId, Guid UserId, int Points);
public record HoldCancelled(Guid OrderId);
public record HoldCancellationFailed(Guid OrderId, string Reason);
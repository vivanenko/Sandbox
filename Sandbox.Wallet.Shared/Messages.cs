namespace Sandbox.Wallet.Shared;

public record HoldCoins(Guid OrderId, Guid UserId, int Points);
public record CoinsHeld(Guid OrderId);
public record CoinsHoldFailed(Guid OrderId, string Reason);
public record CancelHold(Guid OrderId, Guid UserId, int Points);
public record HoldCancelled(Guid OrderId);
public record HoldCancellationFailed(Guid OrderId, string Reason);

public record CommitHold(Guid OrderId, Guid HoldId);
public record HoldCommitted(Guid OrderId);
public record HoldCommitFailed(Guid OrderId, string Reason);
public record RefundCoins(Guid OrderId);
public record CoinsRefunded(Guid OrderId);
public record CoinsRefundFailed(Guid OrderId, string Reason);
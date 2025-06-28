using MassTransit;
using Sandbox.Wallet.Shared;

namespace Sandbox.Wallet;

public class CommitHoldConsumer : IConsumer<CommitHold>
{
    public async Task Consume(ConsumeContext<CommitHold> context)
    {
        var success = true;
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Hold has been committed");
            Console.ResetColor();
            await context.Publish(new HoldCommitted(context.Message.OrderId));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Hold commit failed");
            Console.ResetColor();
            await context.Publish(new HoldCommitFailed(context.Message.OrderId, ""));   
        }
    }
}
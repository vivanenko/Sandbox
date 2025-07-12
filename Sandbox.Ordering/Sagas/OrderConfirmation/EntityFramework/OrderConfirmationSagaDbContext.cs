using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Sandbox.Ordering.Sagas.OrderConfirmation.EntityFramework;

public class OrderConfirmationSagaDbContext(DbContextOptions<OrderConfirmationSagaDbContext> options) 
    : SagaDbContext(options)
{
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderConfirmationStateMap(); }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
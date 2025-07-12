using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Sandbox.Ordering.Sagas.OrderPlacement.EntityFramework;

public class OrderPlacementSagaDbContext(DbContextOptions<OrderPlacementSagaDbContext> options) : SagaDbContext(options)
{
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderPlacementStateMap(); }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
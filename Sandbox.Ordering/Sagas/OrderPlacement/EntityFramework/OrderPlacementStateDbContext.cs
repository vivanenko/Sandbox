using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Sandbox.Ordering.Sagas.OrderPlacement.EntityFramework;

public class OrderPlacementStateDbContext(DbContextOptions options) : SagaDbContext(options)
{
    public const string Schema = "order_placement";
    
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderPlacementStateMap(); }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
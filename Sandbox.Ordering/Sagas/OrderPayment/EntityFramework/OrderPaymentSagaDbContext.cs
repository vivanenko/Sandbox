using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Sandbox.Ordering.Sagas.OrderPayment.EntityFramework;

public class OrderPaymentSagaDbContext(DbContextOptions<OrderPaymentSagaDbContext> options) : SagaDbContext(options)
{
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderPaymentStateMap(); }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
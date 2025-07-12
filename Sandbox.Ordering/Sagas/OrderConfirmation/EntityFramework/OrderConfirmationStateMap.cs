using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sandbox.Ordering.Sagas.OrderConfirmation.EntityFramework;

public class OrderConfirmationStateMap : SagaClassMap<OrderConfirmationState>
{
    protected override void Configure(EntityTypeBuilder<OrderConfirmationState> entity, ModelBuilder model)
    {
        entity.Property(x => x.RowVersion).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
        entity.Property(e => e.CurrentState).HasMaxLength(64);
        entity.Property(e => e.ResponseAddress).HasConversion(uri => uri.ToString(), str => new Uri(str));
    }
}
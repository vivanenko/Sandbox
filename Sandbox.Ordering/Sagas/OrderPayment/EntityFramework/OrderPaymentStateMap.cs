using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sandbox.Ordering.Sagas.OrderPayment.EntityFramework;

public class OrderPaymentStateMap : SagaClassMap<OrderPaymentState>
{
    protected override void Configure(EntityTypeBuilder<OrderPaymentState> entity, ModelBuilder model)
    {
        entity.Property(x => x.RowVersion).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
        entity.Property(e => e.CurrentState).HasMaxLength(64);
        entity.Property(e => e.ResponseAddress).HasConversion(uri => uri.ToString(), str => new Uri(str));
    }
}
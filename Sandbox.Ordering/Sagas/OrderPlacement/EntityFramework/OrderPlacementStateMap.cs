using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sandbox.Ordering.Models;
using Sandbox.Stock.Shared;

namespace Sandbox.Ordering.Sagas.OrderPlacement.EntityFramework;

public class OrderPlacementStateMap : SagaClassMap<OrderPlacementState>
{
    protected override void Configure(EntityTypeBuilder<OrderPlacementState> entity, ModelBuilder model)
    {
        entity.Property(x => x.RowVersion).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
        entity.Property(e => e.CurrentState).HasMaxLength(64);
        entity.Property(e => e.ResponseAddress).HasConversion(uri => uri.ToString(), str => new Uri(str));
        entity.Property(e => e.Items)
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<StockItem[]>(json, (JsonSerializerOptions?)null))
            .HasColumnType("text");
    }
}
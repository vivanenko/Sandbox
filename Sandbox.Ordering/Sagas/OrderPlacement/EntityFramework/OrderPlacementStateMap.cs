using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sandbox.Stock.Shared;

namespace Sandbox.Ordering.Sagas.OrderPlacement.EntityFramework;

public class OrderPlacementStateMap : SagaClassMap<OrderPlacementState>
{
    protected override void Configure(EntityTypeBuilder<OrderPlacementState> entity, ModelBuilder model)
    {
        entity.Property(e => e.CurrentState).HasMaxLength(64);
        entity.Property(e => e.OrderId);
        entity.Property(e => e.UserId);
        entity.Property(e => e.CoinsAmount);
        entity.Property(e => e.Amount);
        entity.Property(e => e.CreatedAt);
        entity.Property(e => e.RequestId);
        entity.Property(e => e.ResponseAddress)
            .HasConversion(
                uri => uri.ToString(),
                str => new Uri(str));

        entity.Property(e => e.Items)
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<ItemDto[]>(json, (JsonSerializerOptions?)null))
            .HasColumnType("text");
    }
}
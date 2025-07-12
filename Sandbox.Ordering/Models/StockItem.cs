using Sandbox.Stock.Shared;

namespace Sandbox.Ordering.Models;

public record StockItem(Guid Id, int Quantity);

public static class StockItemExtensions
{
    public static Item[] ToDtos(this IEnumerable<StockItem> items) =>
        items.Select(x => new Item(x.Id, x.Quantity)).ToArray();
}
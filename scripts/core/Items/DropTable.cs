using Ragna.Core.Combat;

namespace Ragna.Core.Items;

/// <summary>
/// One line of a monster's drop table: <see cref="Chance"/> to drop between <see cref="Min"/> and
/// <see cref="Max"/> of <see cref="Item"/>. Each line is rolled on its own.
/// </summary>
public sealed record DropEntry
{
    /// <summary>Item id in <c>data/items/items.json</c>.</summary>
    public string Item { get; init; } = "";

    /// <summary>Chance from 0 (never) to 1 (always).</summary>
    public double Chance { get; init; } = 1;

    /// <summary>Fewest dropped when it drops. At least 1.</summary>
    public int Min { get; init; } = 1;

    /// <summary>Most dropped when it drops. At least <see cref="Min"/>.</summary>
    public int Max { get; init; } = 1;

    /// <summary>Throws <see cref="ArgumentException"/> if any value is out of range.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Item))
        {
            throw new ArgumentException("drop item is required.");
        }

        if (!(Chance >= 0 && Chance <= 1))
        {
            throw new ArgumentException($"drop '{Item}': chance must be from 0 to 1.");
        }

        if (Min < 1 || Max < Min)
        {
            throw new ArgumentException($"drop '{Item}': min must be at least 1 and max at least min.");
        }
    }
}

/// <summary>Items that fell: <paramref name="Quantity"/> of <paramref name="Item"/>.</summary>
public readonly record struct ItemDrop(string Item, int Quantity);

/// <summary>Rolls a monster's drop table when it dies.</summary>
public static class DropTable
{
    /// <summary>
    /// Rolls every entry once, in order, and returns what drops. One random number decides whether
    /// an entry drops; a second picks the quantity when <see cref="DropEntry.Min"/> and
    /// <see cref="DropEntry.Max"/> differ.
    /// </summary>
    public static IReadOnlyList<ItemDrop> Roll(IEnumerable<DropEntry> entries, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(random);
        var drops = new List<ItemDrop>();
        foreach (var entry in entries)
        {
            if (random.NextDouble() >= entry.Chance)
            {
                continue;
            }

            var quantity = entry.Min;
            if (entry.Max > entry.Min)
            {
                var span = entry.Max - entry.Min + 1;
                quantity += Math.Min((int)(random.NextDouble() * span), span - 1);
            }

            drops.Add(new ItemDrop(entry.Item, quantity));
        }

        return drops;
    }
}

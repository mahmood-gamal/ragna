using Ragna.Core.Items;
using Ragna.Core.Tests.Combat;

namespace Ragna.Core.Tests.Items;

public sealed class DropTableTests
{
    [Fact]
    public void Drops_when_the_roll_is_below_the_chance()
    {
        var entries = new[] { new DropEntry { Item = "gel", Chance = 0.5 } };

        Assert.Equal([new ItemDrop("gel", 1)], DropTable.Roll(entries, new FixedRandom(0.49)));
        Assert.Empty(DropTable.Roll(entries, new FixedRandom(0.5)));
    }

    [Fact]
    public void Chance_one_always_drops_and_chance_zero_never_does()
    {
        var always = new[] { new DropEntry { Item = "a", Chance = 1 } };
        var never = new[] { new DropEntry { Item = "b", Chance = 0 } };

        Assert.Single(DropTable.Roll(always, new FixedRandom(0.999999)));
        Assert.Empty(DropTable.Roll(never, new FixedRandom(0)));
    }

    [Theory]
    [InlineData(0.0, 2)]
    [InlineData(0.33, 2)]
    [InlineData(0.34, 3)]
    [InlineData(0.67, 4)]
    [InlineData(0.999999, 4)]
    public void Quantity_is_spread_from_min_to_max(double roll, int expected)
    {
        var entries = new[] { new DropEntry { Item = "gel", Chance = 1, Min = 2, Max = 4 } };

        var drop = Assert.Single(DropTable.Roll(entries, new FixedRandom(0, roll)));

        Assert.Equal(expected, drop.Quantity);
    }

    [Fact]
    public void Fixed_quantity_uses_one_roll_per_entry()
    {
        var random = new FixedRandom(0);
        var entries = new[]
        {
            new DropEntry { Item = "a", Chance = 1, Min = 3, Max = 3 },
            new DropEntry { Item = "b", Chance = 1 },
        };

        var drops = DropTable.Roll(entries, random);

        Assert.Equal([new ItemDrop("a", 3), new ItemDrop("b", 1)], drops);
        Assert.Equal(2, random.Calls);
    }

    [Fact]
    public void Each_entry_is_rolled_on_its_own_in_order()
    {
        var entries = new[]
        {
            new DropEntry { Item = "a", Chance = 0.5 },
            new DropEntry { Item = "b", Chance = 0.5 },
            new DropEntry { Item = "c", Chance = 0.5 },
        };

        var drops = DropTable.Roll(entries, new FixedRandom(0.9, 0.1, 0.2));

        Assert.Equal([new ItemDrop("b", 1), new ItemDrop("c", 1)], drops);
    }

    [Fact]
    public void Empty_table_drops_nothing()
    {
        Assert.Empty(DropTable.Roll([], new FixedRandom(0)));
    }

    [Theory]
    [InlineData("", 1, 1, 1)]
    [InlineData("a", -0.1, 1, 1)]
    [InlineData("a", 1.1, 1, 1)]
    [InlineData("a", double.NaN, 1, 1)]
    [InlineData("a", 1, 0, 1)]
    [InlineData("a", 1, 3, 2)]
    public void Rejects_bad_entries(string item, double chance, int min, int max)
    {
        var entry = new DropEntry { Item = item, Chance = chance, Min = min, Max = max };

        Assert.Throws<ArgumentException>(entry.Validate);
    }
}

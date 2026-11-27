using Ragna.Core.Stats;

namespace Ragna.Core.Tests.Stats;

public sealed class BaseStatsTests
{
    [Fact]
    public void Indexer_and_with_cover_every_stat()
    {
        var stats = BaseStats.All(1);
        var value = 10;
        foreach (var stat in Enum.GetValues<StatType>())
        {
            stats = stats.With(stat, value);
            Assert.Equal(value, stats[stat]);
            value++;
        }

        Assert.Equal(new BaseStats(10, 11, 12, 13, 14, 15), stats);
    }

    [Fact]
    public void Addition_is_per_stat()
    {
        var sum = new BaseStats(1, 2, 3, 4, 5, 6) + BaseStats.All(1);

        Assert.Equal(new BaseStats(2, 3, 4, 5, 6, 7), sum);
    }
}

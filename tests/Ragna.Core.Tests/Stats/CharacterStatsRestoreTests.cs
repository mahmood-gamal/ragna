using Ragna.Core.Stats;

namespace Ragna.Core.Tests.Stats;

public sealed class CharacterStatsRestoreTests
{
    [Fact]
    public void Restore_sets_everything_in_place_without_a_level_up_event()
    {
        var stats = new CharacterStats(new StatTable());
        var levelUps = 0;
        var changed = 0;
        stats.LevelledUp += _ => levelUps++;
        stats.Changed += () => changed++;

        stats.Restore(12, 40, new BaseStats(5, 6, 7, 8, 9, 10), 3);

        Assert.Equal(12, stats.Level);
        Assert.Equal(40, stats.Exp);
        Assert.Equal(new BaseStats(5, 6, 7, 8, 9, 10), stats.Base);
        Assert.Equal(3, stats.StatPoints);
        Assert.Equal(0, levelUps);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void Restore_rejects_out_of_range_values()
    {
        var stats = new CharacterStats(new StatTable());

        Assert.Throws<ArgumentOutOfRangeException>(() => stats.Restore(0, 0, BaseStats.All(1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stats.Restore(1, 0, BaseStats.All(100), 0));
        Assert.Equal(1, stats.Level);
    }
}

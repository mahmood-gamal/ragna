using Ragna.Core.Stats;

namespace Ragna.Core.Tests.Stats;

public sealed class CharacterStatsTests
{
    private static readonly StatTable Table = new();

    [Fact]
    public void New_character_starts_at_level_one()
    {
        var stats = new CharacterStats(Table);

        Assert.Equal(1, stats.Level);
        Assert.Equal(0, stats.Exp);
        Assert.Equal(48, stats.StatPoints);
        Assert.Equal(BaseStats.All(1), stats.Base);
        Assert.Equal(Table.Exp.ExpToNext(1), stats.ExpToNextLevel);
    }

    [Fact]
    public void Exp_below_the_next_level_is_kept()
    {
        var stats = new CharacterStats(Table);

        Assert.Equal(0, stats.AddExp(stats.ExpToNextLevel - 1));
        Assert.Equal(1, stats.Level);
        Assert.Equal(stats.ExpToNextLevel - 1, stats.Exp);
    }

    [Fact]
    public void Level_up_grants_stat_points_and_carries_exp_over()
    {
        var stats = new CharacterStats(Table);
        var levels = new List<int>();
        stats.LevelledUp += levels.Add;

        Assert.Equal(1, stats.AddExp(Table.Exp.ExpToNext(1) + 3));

        Assert.Equal(2, stats.Level);
        Assert.Equal(3, stats.Exp);
        Assert.Equal(48 + 3, stats.StatPoints);
        Assert.Equal([2], levels);
    }

    [Fact]
    public void Large_exp_gains_several_levels()
    {
        var stats = new CharacterStats(Table);
        var exp = Enumerable.Range(1, 5).Sum(l => Table.Exp.ExpToNext(l));

        Assert.Equal(5, stats.AddExp(exp));

        Assert.Equal(6, stats.Level);
        Assert.Equal(0, stats.Exp);

        // Levels 2, 3, 4 give 3 points; 5 and 6 give 4.
        Assert.Equal(48 + 3 + 3 + 3 + 4 + 4, stats.StatPoints);
    }

    [Fact]
    public void Exp_stops_at_max_level()
    {
        var table = new StatTable { MaxBaseLevel = 3 };
        var stats = new CharacterStats(table);

        Assert.Equal(2, stats.AddExp(long.MaxValue / 2));
        Assert.True(stats.IsMaxLevel);
        Assert.Equal(0, stats.Exp);
        Assert.Equal(0, stats.ExpToNextLevel);
        Assert.Equal(0, stats.AddExp(100));
    }

    [Fact]
    public void Levelling_up_raises_max_hp()
    {
        var stats = new CharacterStats(Table);
        var before = stats.Derived.MaxHp;

        stats.AddExp(stats.ExpToNextLevel);

        Assert.True(stats.Derived.MaxHp > before);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(10, 2)]
    [InlineData(11, 3)]
    [InlineData(91, 11)]
    public void Raise_cost_grows_with_the_stat(int value, int cost)
    {
        var stats = new CharacterStats(Table, 1, 0, BaseStats.All(1).With(StatType.Str, value), 0);

        Assert.Equal(cost, stats.RaiseCost(StatType.Str));
    }

    [Fact]
    public void Raising_a_stat_spends_points_and_updates_derived()
    {
        var stats = new CharacterStats(Table);
        var changes = 0;
        stats.Changed += () => changes++;
        var hpBefore = stats.Derived.MaxHp;

        for (var i = 0; i < 10; i++)
        {
            Assert.True(stats.TryRaise(StatType.Vit));
        }

        Assert.Equal(11, stats.Base.Vit);
        Assert.Equal(48 - 20, stats.StatPoints);
        Assert.Equal(10, changes);
        Assert.True(stats.Derived.MaxHp > hpBefore);
    }

    [Fact]
    public void Cannot_raise_without_points()
    {
        var stats = new CharacterStats(Table, 1, 0, BaseStats.All(1), 1);

        Assert.False(stats.CanRaise(StatType.Agi));
        Assert.False(stats.TryRaise(StatType.Agi));
        Assert.Equal(1, stats.StatPoints);
    }

    [Fact]
    public void Cannot_raise_past_max_stat()
    {
        var stats = new CharacterStats(Table, 1, 0, BaseStats.All(1).With(StatType.Luk, 99), 100);

        Assert.False(stats.TryRaise(StatType.Luk));
    }

    [Fact]
    public void Modifier_sources_change_derived_stats()
    {
        var stats = new CharacterStats(Table);
        var job = new StatModifierSource(
            StatModifier.Percent(DerivedStatType.MaxHp, 100),
            StatModifier.AddStat(StatType.Agi, 10));

        stats.AddModifierSource(job);

        Assert.Equal(70, stats.Derived.MaxHp);
        Assert.Equal(11, stats.Derived.Total.Agi);
        Assert.Equal(1, stats.Base.Agi);

        Assert.True(stats.RemoveModifierSource(job));
        Assert.Equal(35, stats.Derived.MaxHp);
    }

    [Fact]
    public void Restoring_rejects_out_of_range_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterStats(Table, 0, 0, BaseStats.All(1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterStats(Table, 100, 0, BaseStats.All(1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterStats(Table, 1, -1, BaseStats.All(1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterStats(Table, 1, 0, BaseStats.All(0), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterStats(Table, 1, 0, BaseStats.All(100), 0));
    }

    [Fact]
    public void Exp_changed_is_raised_when_exp_is_added()
    {
        var stats = new CharacterStats(Table);
        var changes = 0;
        stats.ExpChanged += () => changes++;

        stats.AddExp(1);
        stats.AddExp(0);
        stats.AddExp(stats.ExpToNextLevel);

        Assert.Equal(2, changes);
    }
}

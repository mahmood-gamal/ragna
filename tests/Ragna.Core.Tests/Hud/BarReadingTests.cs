using Ragna.Core.Combat;
using Ragna.Core.Hud;
using Ragna.Core.Stats;

namespace Ragna.Core.Tests.Hud;

public sealed class BarReadingTests
{
    private static readonly StatTable Table = new();

    [Fact]
    public void Hp_shows_current_over_max()
    {
        var health = new Health(60);
        health.TakeDamage(15);

        var bar = BarReading.Hp(health);

        Assert.Equal(0.75, bar.Fraction, 6);
        Assert.Equal("HP 45 / 60", bar.Text);
    }

    [Fact]
    public void Sp_shows_current_over_max_and_empty_when_there_is_none()
    {
        var sp = new SpPool(20);
        sp.TrySpend(5);

        Assert.Equal(new BarReading(0.75, "SP 15 / 20"), BarReading.Sp(sp));
        Assert.Equal(new BarReading(0, "SP 0 / 0"), BarReading.Sp(new SpPool(0)));
    }

    [Fact]
    public void Exp_shows_progress_to_the_next_level_as_a_percentage()
    {
        var stats = new CharacterStats(Table);
        var needed = stats.ExpToNextLevel;
        stats.AddExp(needed / 2);

        var bar = BarReading.Exp(stats);

        Assert.Equal((double)(needed / 2) / needed, bar.Fraction, 6);
        Assert.StartsWith("EXP ", bar.Text);
        Assert.EndsWith("%", bar.Text);
    }

    [Fact]
    public void Exp_never_reads_100_percent_before_the_level_up()
    {
        var stats = new CharacterStats(Table, 1, 0, BaseStats.All(1), 0);
        stats.AddExp(stats.ExpToNextLevel - 1);

        Assert.NotEqual("EXP 100.0%", BarReading.Exp(stats).Text);
        Assert.True(BarReading.Exp(stats).Fraction < 1);
    }

    [Fact]
    public void Exp_is_full_at_max_level()
    {
        var stats = new CharacterStats(Table, Table.MaxBaseLevel, 0, BaseStats.All(1), 0);

        Assert.Equal(new BarReading(1, "EXP MAX"), BarReading.Exp(stats));
    }
}

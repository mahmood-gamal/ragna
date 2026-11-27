using Ragna.Core.Stats;

namespace Ragna.Core.Tests.Stats;

public sealed class StatCalculatorTests
{
    private static readonly StatTable Table = new();

    [Fact]
    public void Level_one_with_one_in_every_stat()
    {
        var derived = StatCalculator.Compute(Table, 1, BaseStats.All(1));

        // HP: 35 * 1.01, SP: 10 * 1.01, delay: 1 * (1 - 0.005).
        Assert.Equal(35, derived.MaxHp);
        Assert.Equal(10, derived.MaxSp);
        Assert.Equal(1 / 0.995, derived.AttackSpeed, 6);
    }

    [Fact]
    public void Vit_and_level_raise_hp()
    {
        var derived = StatCalculator.Compute(Table, 11, BaseStats.All(1).With(StatType.Vit, 50));

        // (35 + 5 * 10) * 1.5
        Assert.Equal(127, derived.MaxHp);
    }

    [Fact]
    public void Int_and_level_raise_sp()
    {
        var derived = StatCalculator.Compute(Table, 11, BaseStats.All(1).With(StatType.Int, 100));

        // (10 + 10) * 2
        Assert.Equal(40, derived.MaxSp);
    }

    [Fact]
    public void Agi_and_dex_raise_attack_speed()
    {
        var slow = StatCalculator.Compute(Table, 1, BaseStats.All(1));
        var agi = StatCalculator.Compute(Table, 1, BaseStats.All(1).With(StatType.Agi, 50));
        var dex = StatCalculator.Compute(Table, 1, BaseStats.All(1).With(StatType.Dex, 50));

        Assert.True(agi.AttackSpeed > dex.AttackSpeed);
        Assert.True(dex.AttackSpeed > slow.AttackSpeed);

        // 1 - (50 * 0.004 + 1 * 0.001)
        Assert.Equal(0.799, agi.AttackDelay, 6);
    }

    [Fact]
    public void Attack_speed_reduction_is_capped()
    {
        var derived = StatCalculator.Compute(Table, 1, BaseStats.All(99).With(StatType.Agi, 500));

        Assert.Equal(0.4, derived.AttackDelay, 6);
    }

    [Fact]
    public void Stat_modifiers_feed_the_formulas()
    {
        var derived = StatCalculator.Compute(
            Table,
            1,
            BaseStats.All(1),
            [StatModifier.AddStat(StatType.Vit, 99)]);

        Assert.Equal(100, derived.Total.Vit);
        Assert.Equal(70, derived.MaxHp);
    }

    [Fact]
    public void Flat_modifiers_apply_before_percent()
    {
        var derived = StatCalculator.Compute(
            Table,
            1,
            BaseStats.All(1),
            [
                StatModifier.Flat(DerivedStatType.MaxHp, 65),
                StatModifier.Percent(DerivedStatType.MaxHp, 50),
                StatModifier.Percent(DerivedStatType.MaxHp, 50),
            ]);

        // (35.35 + 65) * 2
        Assert.Equal(200, derived.MaxHp);
    }

    [Fact]
    public void Attack_speed_never_exceeds_the_min_delay()
    {
        var derived = StatCalculator.Compute(
            Table,
            1,
            BaseStats.All(1),
            [StatModifier.Percent(DerivedStatType.AttackSpeed, 10_000)]);

        Assert.Equal(5, derived.AttackSpeed, 6);
    }

    [Fact]
    public void Hp_never_drops_below_one()
    {
        var derived = StatCalculator.Compute(
            Table,
            1,
            BaseStats.All(1),
            [StatModifier.Flat(DerivedStatType.MaxHp, -1000), StatModifier.Flat(DerivedStatType.MaxSp, -1000)]);

        Assert.Equal(1, derived.MaxHp);
        Assert.Equal(0, derived.MaxSp);
    }
}

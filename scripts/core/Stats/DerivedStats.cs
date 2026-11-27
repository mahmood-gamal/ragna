namespace Ragna.Core.Stats;

/// <summary>Stats computed from base stats, level and modifiers.</summary>
/// <param name="Total">Base stats plus stat bonuses from modifiers.</param>
/// <param name="MaxHp">Maximum HP, at least 1.</param>
/// <param name="MaxSp">Maximum SP, at least 0.</param>
/// <param name="AttackSpeed">Attacks per second.</param>
public sealed record DerivedStats(BaseStats Total, int MaxHp, int MaxSp, double AttackSpeed)
{
    /// <summary>Seconds between two attacks.</summary>
    public double AttackDelay => 1.0 / AttackSpeed;
}

/// <summary>Turns base stats, level and modifiers into <see cref="DerivedStats"/>.</summary>
public static class StatCalculator
{
    public static DerivedStats Compute(
        StatTable table,
        int level,
        BaseStats baseStats,
        IEnumerable<StatModifier>? modifiers = null)
    {
        var mods = modifiers?.ToArray() ?? [];

        var total = baseStats;
        foreach (var mod in mods)
        {
            if (mod.Stat is { } stat)
            {
                total = total.With(stat, total[stat] + (int)mod.Value);
            }
        }

        var hp = Apply(table.Hp.Compute(level, Math.Max(0, total.Vit)), DerivedStatType.MaxHp, mods);
        var sp = Apply(table.Sp.Compute(level, Math.Max(0, total.Int)), DerivedStatType.MaxSp, mods);
        var aspd = Apply(
            table.AttackSpeed.Compute(Math.Max(0, total.Agi), Math.Max(0, total.Dex)),
            DerivedStatType.AttackSpeed,
            mods);

        var maxAspd = 1.0 / table.AttackSpeed.MinDelay;
        return new DerivedStats(
            total,
            Math.Max(1, (int)Math.Floor(hp)),
            Math.Max(0, (int)Math.Floor(sp)),
            Math.Clamp(aspd, 0.01, maxAspd));
    }

    private static double Apply(double value, DerivedStatType target, StatModifier[] mods)
    {
        double flat = 0, percent = 0;
        foreach (var mod in mods)
        {
            if (mod.Derived != target)
            {
                continue;
            }

            if (mod.Kind == ModifierKind.Flat)
            {
                flat += mod.Value;
            }
            else
            {
                percent += mod.Value;
            }
        }

        return (value + flat) * (1 + percent / 100.0);
    }
}

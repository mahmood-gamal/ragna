using System.Globalization;
using Ragna.Core.Combat;
using Ragna.Core.Stats;

namespace Ragna.Core.Hud;

/// <summary>What one HUD bar shows: how full it is and its caption.</summary>
/// <param name="Fraction">How full the bar is, from 0 to 1.</param>
/// <param name="Text">Caption drawn on the bar, for example <c>HP 45 / 60</c>.</param>
public readonly record struct BarReading(double Fraction, string Text)
{
    public static BarReading Hp(Health health)
    {
        ArgumentNullException.ThrowIfNull(health);
        return new(Math.Clamp(health.Fraction, 0, 1), $"HP {health.Current} / {health.Max}");
    }

    public static BarReading Sp(SpPool sp)
    {
        ArgumentNullException.ThrowIfNull(sp);
        return new(Math.Clamp(sp.Fraction, 0, 1), $"SP {sp.Current} / {sp.Max}");
    }

    /// <summary>EXP towards the next level, as a percentage like Ragnarok. Full at max level.</summary>
    public static BarReading Exp(CharacterStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        if (stats.IsMaxLevel || stats.ExpToNextLevel <= 0)
        {
            return new(1, "EXP MAX");
        }

        var fraction = Math.Clamp((double)stats.Exp / stats.ExpToNextLevel, 0, 1);
        // Round down so the bar never reads 100.0% before the level-up.
        var percent = Math.Floor(fraction * 1000) / 10;
        return new(fraction, $"EXP {percent.ToString("0.0", CultureInfo.InvariantCulture)}%");
    }
}

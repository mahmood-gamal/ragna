using Ragna.Core.Combat;

namespace Ragna.Core.Hud;

/// <summary>
/// The filled part of a bar drawn in the world, such as the HP bar over the player's target. The
/// fill is a quad anchored at the bar's left edge, so it shrinks towards the left as HP drops.
/// </summary>
/// <param name="Fraction">How full the bar is, from 0 to 1.</param>
/// <param name="Width">Width of the filled quad.</param>
/// <param name="CenterX">Centre of the filled quad, relative to the centre of the whole bar.</param>
public readonly record struct WorldBarFill(double Fraction, float Width, float CenterX)
{
    /// <summary>The fill of a bar <paramref name="barWidth"/> wide that is <paramref name="fraction"/> full.</summary>
    public static WorldBarFill For(double fraction, float barWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(barWidth);
        var f = double.IsNaN(fraction) ? 0 : Math.Clamp(fraction, 0, 1);
        var width = (float)(barWidth * f);
        return new(f, width, (width - barWidth) / 2f);
    }

    /// <summary>The fill for <paramref name="health"/>.</summary>
    public static WorldBarFill For(Health health, float barWidth)
    {
        ArgumentNullException.ThrowIfNull(health);
        return For(health.Fraction, barWidth);
    }
}

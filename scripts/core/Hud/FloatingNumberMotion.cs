namespace Ragna.Core.Hud;

/// <summary>Where a floating number is, and how it looks, at one moment of its animation.</summary>
/// <param name="OffsetX">Sideways drift from where it spawned, in metres.</param>
/// <param name="OffsetY">Height above where it spawned, in metres.</param>
/// <param name="Alpha">Opacity, from 1 to 0.</param>
/// <param name="Scale">Size multiplier. Starts large and settles at 1.</param>
public readonly record struct FloatingNumberSample(float OffsetX, float OffsetY, float Alpha, float Scale);

/// <summary>
/// The animation of a floating damage number: it pops in large, rises and drifts to one side
/// while slowing down, then fades out. Every value is a pure function of the time since it spawned.
/// </summary>
public sealed record FloatingNumberMotion
{
    /// <summary>Seconds from spawn until the number is gone.</summary>
    public float Duration { get; init; } = 0.9f;

    /// <summary>How far it rises, in metres.</summary>
    public float Rise { get; init; } = 0.8f;

    /// <summary>How far it drifts sideways, in metres.</summary>
    public float Drift { get; init; } = 0.3f;

    /// <summary>Size when it appears.</summary>
    public float PopScale { get; init; } = 1.4f;

    /// <summary>Seconds it takes to shrink from <see cref="PopScale"/> to normal size.</summary>
    public float PopTime { get; init; } = 0.12f;

    /// <summary>Part of <see cref="Duration"/>, from 0 to 1, after which it starts fading.</summary>
    public float FadeStart { get; init; } = 0.55f;

    /// <summary>Whether the number has finished and can be removed.</summary>
    public bool IsFinished(float elapsed) => elapsed >= Duration;

    /// <summary>
    /// The number <paramref name="elapsed"/> seconds after spawning. <paramref name="driftSign"/>
    /// picks the side it drifts to (negative for left, positive for right, 0 for straight up), so
    /// numbers from quick hits on one target don't stack on top of each other.
    /// </summary>
    public FloatingNumberSample Sample(float elapsed, int driftSign)
    {
        var t = Duration <= 0f ? 1f : Math.Clamp(elapsed / Duration, 0f, 1f);

        // Ease out: fast at first, slowing to a stop.
        var eased = 1f - ((1f - t) * (1f - t));
        var y = Rise * eased;
        var x = Drift * Math.Sign(driftSign) * eased;

        var alpha = t <= FadeStart || FadeStart >= 1f ? 1f : 1f - ((t - FadeStart) / (1f - FadeStart));

        var pop = PopTime <= 0f ? 1f : Math.Clamp(elapsed / PopTime, 0f, 1f);
        var scale = PopScale + ((1f - PopScale) * pop);

        return new(x, y, Math.Clamp(alpha, 0f, 1f), scale);
    }
}

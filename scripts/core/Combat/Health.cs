namespace Ragna.Core.Combat;

/// <summary>Current and maximum HP of anything that can be attacked.</summary>
public sealed class Health
{
    public Health(int max)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(max, 1);
        Max = max;
        Current = max;
    }

    /// <summary>Raised after HP drops, with the damage taken.</summary>
    public event Action<int>? Damaged;

    /// <summary>Raised once, when HP reaches 0.</summary>
    public event Action? Died;

    public int Max { get; private set; }

    public int Current { get; private set; }

    public bool IsDead => Current <= 0;

    /// <summary>HP left, from 0 to 1.</summary>
    public double Fraction => (double)Current / Max;

    /// <summary>Takes up to <paramref name="amount"/> HP. Returns the HP actually lost. Does nothing once dead.</summary>
    public int TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (IsDead || amount == 0)
        {
            return 0;
        }

        var lost = Math.Min(amount, Current);
        Current -= lost;
        Damaged?.Invoke(lost);
        if (IsDead)
        {
            Died?.Invoke();
        }

        return lost;
    }

    /// <summary>Restores up to <paramref name="amount"/> HP. Returns the HP actually gained. The dead can't be healed.</summary>
    public int Heal(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (IsDead)
        {
            return 0;
        }

        var gained = Math.Min(amount, Max - Current);
        Current += gained;
        return gained;
    }

    /// <summary>Changes max HP, keeping the same HP missing. Current HP stays at least 1 while alive.</summary>
    public void SetMax(int max)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(max, 1);
        if (IsDead)
        {
            Max = max;
            return;
        }

        var missing = Max - Current;
        Max = max;
        Current = Math.Clamp(max - missing, 1, max);
    }

    /// <summary>Back to full HP, for example on respawn.</summary>
    public void Revive() => Current = Max;
}

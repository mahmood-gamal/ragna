namespace Ragna.Core.Combat;

/// <summary>
/// Current and maximum SP, spent on skills. Unlike <see cref="Health"/>, max SP can be 0 (a
/// character with no INT at level 1 may have none).
/// </summary>
public sealed class SpPool
{
    public SpPool(int max)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(max);
        Max = max;
        Current = max;
    }

    /// <summary>Raised whenever <see cref="Current"/> or <see cref="Max"/> changes, for SP bars.</summary>
    public event Action? Changed;

    public int Max { get; private set; }

    public int Current { get; private set; }

    /// <summary>SP left, from 0 to 1. 0 when <see cref="Max"/> is 0.</summary>
    public double Fraction => Max == 0 ? 0 : (double)Current / Max;

    /// <summary>Spends <paramref name="amount"/> SP if there is enough. Returns false, spending nothing, if not.</summary>
    public bool TrySpend(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (amount > Current)
        {
            return false;
        }

        if (amount > 0)
        {
            Current -= amount;
            Changed?.Invoke();
        }

        return true;
    }

    /// <summary>Restores up to <paramref name="amount"/> SP. Returns the SP actually gained.</summary>
    public int Restore(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var gained = Math.Min(amount, Max - Current);
        if (gained > 0)
        {
            Current += gained;
            Changed?.Invoke();
        }

        return gained;
    }

    /// <summary>Changes max SP, keeping the same SP missing, never below 0.</summary>
    public void SetMax(int max)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(max);
        var missing = Max - Current;
        Max = max;
        Current = Math.Clamp(max - missing, 0, max);
        Changed?.Invoke();
    }

    /// <summary>Back to full SP, for example on respawn.</summary>
    public void Refill()
    {
        Current = Max;
        Changed?.Invoke();
    }
}

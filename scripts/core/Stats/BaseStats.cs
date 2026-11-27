namespace Ragna.Core.Stats;

/// <summary>Immutable set of the six base stats.</summary>
public readonly record struct BaseStats(int Str, int Agi, int Vit, int Int, int Dex, int Luk)
{
    /// <summary>All stats at <paramref name="value"/>.</summary>
    public static BaseStats All(int value) => new(value, value, value, value, value, value);

    public int this[StatType stat] => stat switch
    {
        StatType.Str => Str,
        StatType.Agi => Agi,
        StatType.Vit => Vit,
        StatType.Int => Int,
        StatType.Dex => Dex,
        StatType.Luk => Luk,
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    /// <summary>A copy with <paramref name="stat"/> set to <paramref name="value"/>.</summary>
    public BaseStats With(StatType stat, int value) => stat switch
    {
        StatType.Str => this with { Str = value },
        StatType.Agi => this with { Agi = value },
        StatType.Vit => this with { Vit = value },
        StatType.Int => this with { Int = value },
        StatType.Dex => this with { Dex = value },
        StatType.Luk => this with { Luk = value },
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    /// <summary>The stats added together, one by one.</summary>
    public static BaseStats operator +(BaseStats a, BaseStats b) =>
        new(a.Str + b.Str, a.Agi + b.Agi, a.Vit + b.Vit, a.Int + b.Int, a.Dex + b.Dex, a.Luk + b.Luk);
}

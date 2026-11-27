namespace Ragna.Core.Stats;

/// <summary>How a <see cref="StatModifier"/> changes its target.</summary>
public enum ModifierKind
{
    /// <summary>Adds <see cref="StatModifier.Value"/> to the target.</summary>
    Flat,

    /// <summary>
    /// Adds <see cref="StatModifier.Value"/> percent of the target. Percent modifiers on the same
    /// target add up, then apply once after all flat modifiers.
    /// </summary>
    Percent,
}

/// <summary>
/// A bonus from a job class, skill, buff or item. It targets either a base stat
/// (<see cref="Stat"/>) or a derived stat (<see cref="Derived"/>), never both.
/// </summary>
public sealed record StatModifier
{
    private StatModifier(StatType? stat, DerivedStatType? derived, ModifierKind kind, double value)
    {
        Stat = stat;
        Derived = derived;
        Kind = kind;
        Value = value;
    }

    public StatType? Stat { get; }

    public DerivedStatType? Derived { get; }

    public ModifierKind Kind { get; }

    public double Value { get; }

    /// <summary>Adds <paramref name="value"/> points to a base stat.</summary>
    public static StatModifier AddStat(StatType stat, int value) => new(stat, null, ModifierKind.Flat, value);

    /// <summary>Changes a derived stat by a flat amount.</summary>
    public static StatModifier Flat(DerivedStatType derived, double value) => new(null, derived, ModifierKind.Flat, value);

    /// <summary>Changes a derived stat by <paramref name="percent"/> percent.</summary>
    public static StatModifier Percent(DerivedStatType derived, double percent) =>
        new(null, derived, ModifierKind.Percent, percent);
}

/// <summary>
/// Anything that changes a character's stats: a job class, a passive skill, a buff, equipment.
/// Add one to <see cref="CharacterStats.ModifierSources"/> and the derived stats update.
/// </summary>
public interface IStatModifierSource
{
    IEnumerable<StatModifier> Modifiers { get; }
}

/// <summary>A fixed list of modifiers.</summary>
public sealed class StatModifierSource(IEnumerable<StatModifier> modifiers) : IStatModifierSource
{
    private readonly StatModifier[] _modifiers = modifiers.ToArray();

    public StatModifierSource(params StatModifier[] modifiers)
        : this((IEnumerable<StatModifier>)modifiers)
    {
    }

    public IEnumerable<StatModifier> Modifiers => _modifiers;
}

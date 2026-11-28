namespace Ragna.Core.Combat;

/// <summary>Tuning numbers shared by every attack. Percentages are 0–100.</summary>
public sealed record CombatRules
{
    public static CombatRules Default { get; } = new();

    /// <summary>Chance to hit when the attacker's HIT equals the target's FLEE.</summary>
    public double BaseHitChance { get; init; } = 80;

    /// <summary>Lowest chance to hit, however high the target's FLEE.</summary>
    public double MinHitChance { get; init; } = 5;

    /// <summary>Highest chance to hit, however high the attacker's HIT.</summary>
    public double MaxHitChance { get; init; } = 100;

    /// <summary>Critical chance every character has, before LUK.</summary>
    public double BaseCritChance { get; init; } = 1;

    /// <summary>Critical chance each point of LUK adds.</summary>
    public double CritPerLuk { get; init; } = 0.3;

    /// <summary>Damage multiplier of a critical hit.</summary>
    public double CritMultiplier { get; init; } = 1.4;

    /// <summary>Whether critical hits ignore the target's DEF.</summary>
    public bool CritIgnoresDef { get; init; } = true;

    /// <summary>
    /// Random spread of ATK: each attack rolls between ATK × (1 − variance) and ATK × (1 + variance).
    /// 0.1 is ±10%.
    /// </summary>
    public double AtkVariance { get; init; } = 0.1;

    /// <summary>Damage a hit always deals, however high the target's DEF.</summary>
    public int MinDamage { get; init; } = 1;
}

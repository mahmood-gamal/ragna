namespace Ragna.Core.Combat;

/// <summary>
/// What kind of attack is being made. A normal auto-attack uses <see cref="NormalAttack"/>; a skill
/// builds its own, for example <c>new AttackContext { Name = "Bash", SkillMultiplier = 3 }</c>.
/// </summary>
public sealed record AttackContext
{
    public static AttackContext NormalAttack { get; } = new();

    /// <summary>Shown in logs, for example the skill's name.</summary>
    public string Name { get; init; } = "Attack";

    /// <summary>Multiplies the damage after DEF. 1 is a normal attack; 3 is a 300% skill.</summary>
    public double SkillMultiplier { get; init; } = 1;

    /// <summary>Added to ATK before DEF.</summary>
    public int FlatBonus { get; init; }

    /// <summary>Added to the attacker's HIT for this attack only.</summary>
    public int HitBonus { get; init; }

    /// <summary>Whether this attack can land a critical hit. Most skills can't.</summary>
    public bool CanCrit { get; init; } = true;

    /// <summary>Whether this attack ignores FLEE and always lands.</summary>
    public bool AlwaysHits { get; init; }

    /// <summary>Applied in order after the built-in formula, for example elements or card effects.</summary>
    public IReadOnlyList<IDamageModifier> Modifiers { get; init; } = [];
}

/// <summary>Changes an attack's damage after the built-in formula, for example an element bonus.</summary>
public interface IDamageModifier
{
    /// <summary>
    /// Returns the new damage. <paramref name="damage"/> is the damage so far, before rounding and
    /// before <see cref="CombatRules.MinDamage"/>. Only called for attacks that land.
    /// </summary>
    double Modify(double damage, CombatStats attacker, CombatStats defender, AttackOutcome outcome);
}

namespace Ragna.Core.Combat;

/// <summary>
/// Works out one attack. Each call rolls, in order: critical (if the attack can crit), then hit (if
/// it didn't crit), then the ATK spread.
/// <list type="number">
/// <item>Critical hit: chance is the attacker's CRIT. A critical always lands.</item>
/// <item>Hit chance: <c>clamp(BaseHitChance + HIT + HitBonus − FLEE, MinHitChance, MaxHitChance)</c>.</item>
/// <item>ATK roll: <c>(ATK + FlatBonus) × (1 ± AtkVariance)</c>.</item>
/// <item>DEF: subtracted, unless it was a critical and <see cref="CombatRules.CritIgnoresDef"/>.</item>
/// <item>× <see cref="AttackContext.SkillMultiplier"/>, × <see cref="CombatRules.CritMultiplier"/> on a critical.</item>
/// <item>Each <see cref="IDamageModifier"/> in turn.</item>
/// <item>Rounded down, at least <see cref="CombatRules.MinDamage"/>.</item>
/// </list>
/// </summary>
public sealed class DamageCalculator(CombatRules? rules = null, IRandomSource? random = null)
{
    public CombatRules Rules { get; } = rules ?? CombatRules.Default;

    public IRandomSource Random { get; } = random ?? SystemRandomSource.Shared;

    /// <summary>Chance, in percent, that a non-critical attack lands.</summary>
    public double HitChance(CombatStats attacker, CombatStats defender, AttackContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);
        context ??= AttackContext.NormalAttack;
        if (context.AlwaysHits)
        {
            return 100;
        }

        var chance = Rules.BaseHitChance + attacker.Hit + context.HitBonus - defender.Flee;
        return Math.Clamp(chance, Rules.MinHitChance, Rules.MaxHitChance);
    }

    public AttackResult Attack(CombatStats attacker, CombatStats defender, AttackContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);
        context ??= AttackContext.NormalAttack;

        var critical = context.CanCrit && Roll(attacker.CritChance);
        if (!critical && !Roll(HitChance(attacker, defender, context)))
        {
            return AttackResult.Miss;
        }

        var outcome = critical ? AttackOutcome.Critical : AttackOutcome.Hit;
        var spread = ((Random.NextDouble() * 2) - 1) * Rules.AtkVariance;
        var damage = Math.Max(0, attacker.Atk + context.FlatBonus) * (1 + spread);

        if (!(critical && Rules.CritIgnoresDef))
        {
            damage -= Math.Max(0, defender.Def);
        }

        damage *= context.SkillMultiplier;
        if (critical)
        {
            damage *= Rules.CritMultiplier;
        }

        foreach (var modifier in context.Modifiers)
        {
            damage = modifier.Modify(damage, attacker, defender, outcome);
        }

        var final = double.IsFinite(damage) ? (int)Math.Floor(Math.Min(damage, int.MaxValue)) : Rules.MinDamage;
        return new AttackResult(outcome, Math.Max(Rules.MinDamage, final));
    }

    // True with a chance of `percent` out of 100. Always uses one number, so the order of rolls
    // doesn't depend on the stats.
    private bool Roll(double percent) => Random.NextDouble() * 100 < percent;
}

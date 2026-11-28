using Ragna.Core.Stats;

namespace Ragna.Core.Combat;

/// <summary>
/// The numbers an attack is worked out from. Players get theirs from <see cref="FromCharacter"/>;
/// monsters set them directly.
/// </summary>
/// <param name="Level">Base level. Adds to <see cref="Hit"/> and <see cref="Flee"/> for players.</param>
/// <param name="Atk">Attack power before defence.</param>
/// <param name="Def">Flat damage taken off every hit.</param>
/// <param name="Hit">Accuracy. Each point over the target's <see cref="Flee"/> is +1% to hit.</param>
/// <param name="Flee">Evasion against <see cref="Hit"/>.</param>
/// <param name="CritChance">Chance to land a critical hit, in percent.</param>
public sealed record CombatStats(int Level, int Atk, int Def, int Hit, int Flee, double CritChance)
{
    /// <summary>
    /// Combat stats for a character, using <paramref name="rules"/> (or the defaults):
    /// <list type="bullet">
    /// <item>ATK = STR + floor(STR / 10)² + floor(DEX / 5) + floor(LUK / 5) + <paramref name="weaponAtk"/></item>
    /// <item>DEF = floor(VIT / 2) + <paramref name="armorDef"/></item>
    /// <item>HIT = level + DEX</item>
    /// <item>FLEE = level + AGI</item>
    /// <item>CRIT = <see cref="CombatRules.BaseCritChance"/> + LUK × <see cref="CombatRules.CritPerLuk"/> %</item>
    /// </list>
    /// Uses the character's total stats, so job classes and buffs count.
    /// </summary>
    public static CombatStats FromCharacter(
        CharacterStats character,
        int weaponAtk = 0,
        int armorDef = 0,
        CombatRules? rules = null)
    {
        ArgumentNullException.ThrowIfNull(character);
        rules ??= CombatRules.Default;

        var s = character.Derived.Total;
        var str = Math.Max(0, s.Str);
        var agi = Math.Max(0, s.Agi);
        var vit = Math.Max(0, s.Vit);
        var dex = Math.Max(0, s.Dex);
        var luk = Math.Max(0, s.Luk);
        var strBonus = str / 10;

        return new CombatStats(
            character.Level,
            Atk: str + (strBonus * strBonus) + (dex / 5) + (luk / 5) + weaponAtk,
            Def: (vit / 2) + armorDef,
            Hit: character.Level + dex,
            Flee: character.Level + agi,
            CritChance: rules.BaseCritChance + (luk * rules.CritPerLuk));
    }
}

using Ragna.Core.Combat;
using Ragna.Core.Stats;

namespace Ragna.Core.Tests.Combat;

public sealed class CombatStatsTests
{
    private static readonly StatTable Table = new();

    [Fact]
    public void New_character_has_small_combat_stats()
    {
        var stats = CombatStats.FromCharacter(new CharacterStats(Table));

        Assert.Equal(new CombatStats(Level: 1, Atk: 1, Def: 0, Hit: 2, Flee: 2, CritChance: 1.3), stats with { CritChance = Math.Round(stats.CritChance, 6) });
    }

    [Fact]
    public void Stats_feed_the_formulas()
    {
        var character = new CharacterStats(Table, 10, 0, new BaseStats(Str: 25, Agi: 12, Vit: 9, Int: 1, Dex: 17, Luk: 10), 0);

        var stats = CombatStats.FromCharacter(character, weaponAtk: 20, armorDef: 3);

        Assert.Equal(10, stats.Level);
        Assert.Equal(25 + 4 + 3 + 2 + 20, stats.Atk); // STR + (25/10)² + DEX/5 + LUK/5 + weapon
        Assert.Equal(4 + 3, stats.Def);               // VIT/2 + armour
        Assert.Equal(10 + 17, stats.Hit);
        Assert.Equal(10 + 12, stats.Flee);
        Assert.Equal(1 + 3, stats.CritChance, 6);
    }

    [Fact]
    public void Stat_bonuses_from_modifiers_count()
    {
        var character = new CharacterStats(Table);
        character.AddModifierSource(new Source(StatModifier.AddStat(StatType.Str, 9)));

        Assert.Equal(10 + 1, CombatStats.FromCharacter(character).Atk); // STR 10 → 10 + 1²
    }

    [Fact]
    public void Custom_rules_change_crit()
    {
        var rules = new CombatRules { BaseCritChance = 0, CritPerLuk = 1 };
        var character = new CharacterStats(Table, 1, 0, BaseStats.All(1) with { Luk = 20 }, 0);

        Assert.Equal(20, CombatStats.FromCharacter(character, rules: rules).CritChance, 6);
    }

    private sealed class Source(params StatModifier[] modifiers) : IStatModifierSource
    {
        public IEnumerable<StatModifier> Modifiers => modifiers;
    }
}

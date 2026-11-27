namespace Ragna.Core.Stats;

/// <summary>The six base stats a character raises with stat points.</summary>
public enum StatType
{
    /// <summary>Strength: melee damage and carry weight.</summary>
    Str,

    /// <summary>Agility: attack speed and flee.</summary>
    Agi,

    /// <summary>Vitality: max HP and HP regeneration.</summary>
    Vit,

    /// <summary>Intelligence: max SP and magic damage.</summary>
    Int,

    /// <summary>Dexterity: hit, ranged damage and a little attack speed.</summary>
    Dex,

    /// <summary>Luck: critical hits and perfect dodge.</summary>
    Luk,
}

/// <summary>Values derived from base stats and level.</summary>
public enum DerivedStatType
{
    MaxHp,
    MaxSp,

    /// <summary>Attacks per second.</summary>
    AttackSpeed,
}

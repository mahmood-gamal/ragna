namespace Ragna.Core.Stats;

/// <summary>
/// A character's level, EXP, unspent stat points and base stats. Gaining EXP levels the character
/// up and grants stat points; spending points raises base stats. <see cref="Derived"/> always
/// reflects the current values and <see cref="ModifierSources"/> (job class, skills, buffs).
/// </summary>
public sealed class CharacterStats
{
    private readonly List<IStatModifierSource> _sources = [];
    private DerivedStats? _derived;

    /// <summary>A new level-1 character with every stat at the table's starting value.</summary>
    public CharacterStats(StatTable table)
        : this(table, 1, 0, BaseStats.All(table.StartingStatValue), table.StartingStatPoints)
    {
    }

    /// <summary>Restores a character, for example from a save.</summary>
    public CharacterStats(StatTable table, int level, long exp, BaseStats baseStats, int statPoints)
    {
        ArgumentNullException.ThrowIfNull(table);
        Check(table, level, exp, baseStats, statPoints);
        Table = table;
        Level = level;
        Exp = level == table.MaxBaseLevel ? 0 : exp;
        Base = baseStats;
        StatPoints = statPoints;
    }

    /// <summary>Raised after one or more level-ups, with the new level.</summary>
    public event Action<int>? LevelledUp;

    /// <summary>Raised after <see cref="AddExp"/> changes <see cref="Exp"/> or <see cref="Level"/>, for EXP bars.</summary>
    public event Action? ExpChanged;

    /// <summary>Raised whenever <see cref="Derived"/> may have changed.</summary>
    public event Action? Changed;

    public StatTable Table { get; }

    /// <summary>Base level, from 1 to <see cref="StatTable.MaxBaseLevel"/>.</summary>
    public int Level { get; private set; }

    /// <summary>EXP gathered towards the next level. Always 0 at max level.</summary>
    public long Exp { get; private set; }

    /// <summary>EXP needed for the next level, or 0 at max level.</summary>
    public long ExpToNextLevel => IsMaxLevel ? 0 : Table.Exp.ExpToNext(Level);

    public bool IsMaxLevel => Level >= Table.MaxBaseLevel;

    /// <summary>Unspent stat points.</summary>
    public int StatPoints { get; private set; }

    /// <summary>Base stats raised with stat points, without modifiers.</summary>
    public BaseStats Base { get; private set; }

    /// <summary>Job class, skills, buffs and equipment that change stats.</summary>
    public IReadOnlyList<IStatModifierSource> ModifierSources => _sources;

    /// <summary>Derived stats for the current level, base stats and modifiers.</summary>
    public DerivedStats Derived => _derived ??= StatCalculator.Compute(
        Table,
        Level,
        Base,
        _sources.SelectMany(s => s.Modifiers));

    /// <summary>
    /// Stat points it costs to raise <paramref name="stat"/> by one from its current value:
    /// <c>2 + floor((value - 1) / 10)</c>, so higher stats cost more.
    /// </summary>
    public int RaiseCost(StatType stat) => 2 + (Base[stat] - 1) / 10;

    public bool CanRaise(StatType stat) => Base[stat] < Table.MaxStatValue && StatPoints >= RaiseCost(stat);

    /// <summary>Spends stat points to raise <paramref name="stat"/> by one. Returns false if it can't.</summary>
    public bool TryRaise(StatType stat)
    {
        if (!CanRaise(stat))
        {
            return false;
        }

        StatPoints -= RaiseCost(stat);
        Base = Base.With(stat, Base[stat] + 1);
        Invalidate();
        return true;
    }

    /// <summary>
    /// Adds EXP and levels up as many times as it covers. Each level-up grants stat points. EXP
    /// past max level is dropped. Returns the number of levels gained.
    /// </summary>
    public int AddExp(long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (IsMaxLevel || amount == 0)
        {
            return 0;
        }

        var gained = 0;
        Exp += amount;
        while (!IsMaxLevel && Exp >= ExpToNextLevel)
        {
            Exp -= ExpToNextLevel;
            Level++;
            StatPoints += Table.StatPoints.PointsForReaching(Level);
            gained++;
        }

        if (IsMaxLevel)
        {
            Exp = 0;
        }

        if (gained > 0)
        {
            Invalidate();
            LevelledUp?.Invoke(Level);
        }

        ExpChanged?.Invoke();
        return gained;
    }

    /// <summary>
    /// Sets level, EXP, base stats and stat points in place, for example when loading a save, so
    /// anything listening to this object keeps working. Modifier sources stay. Raises
    /// <see cref="Changed"/> and <see cref="ExpChanged"/>, not <see cref="LevelledUp"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of the table's range; nothing changes.</exception>
    public void Restore(int level, long exp, BaseStats baseStats, int statPoints)
    {
        Check(Table, level, exp, baseStats, statPoints);
        Level = level;
        Exp = level == Table.MaxBaseLevel ? 0 : exp;
        Base = baseStats;
        StatPoints = statPoints;
        Invalidate();
        ExpChanged?.Invoke();
    }

    public void AddModifierSource(IStatModifierSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _sources.Add(source);
        Invalidate();
    }

    public bool RemoveModifierSource(IStatModifierSource source)
    {
        var removed = _sources.Remove(source);
        if (removed)
        {
            Invalidate();
        }

        return removed;
    }

    private static void Check(StatTable table, int level, long exp, BaseStats baseStats, int statPoints)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, table.MaxBaseLevel);
        ArgumentOutOfRangeException.ThrowIfNegative(exp);
        ArgumentOutOfRangeException.ThrowIfNegative(statPoints);
        foreach (var stat in Enum.GetValues<StatType>())
        {
            if (baseStats[stat] < table.StartingStatValue || baseStats[stat] > table.MaxStatValue)
            {
                throw new ArgumentOutOfRangeException(nameof(baseStats), $"{stat} is out of range.");
            }
        }
    }

    /// <summary>Call when a source's modifiers change, for example a skill levelled up.</summary>
    public void Invalidate()
    {
        _derived = null;
        Changed?.Invoke();
    }
}

namespace Ragna.Core.Stats;

/// <summary>
/// Tuning numbers for stats, levels and EXP. Loaded from JSON with <see cref="StatTableLoader"/>
/// (the game uses <c>data/stats/stat_table.json</c>). A job class can bring its own table.
/// </summary>
public sealed record StatTable
{
    /// <summary>Highest base level.</summary>
    public int MaxBaseLevel { get; init; } = 99;

    /// <summary>Highest value a base stat can be raised to with stat points.</summary>
    public int MaxStatValue { get; init; } = 99;

    /// <summary>Value every base stat starts at.</summary>
    public int StartingStatValue { get; init; } = 1;

    /// <summary>Stat points a new level-1 character has to spend.</summary>
    public int StartingStatPoints { get; init; } = 48;

    public ExpCurve Exp { get; init; } = new();

    public StatPointRules StatPoints { get; init; } = new();

    public ResourceFormula Hp { get; init; } = new() { Base = 35, PerLevel = 5, PercentPerStat = 1 };

    public ResourceFormula Sp { get; init; } = new() { Base = 10, PerLevel = 1, PercentPerStat = 1 };

    public AttackSpeedFormula AttackSpeed { get; init; } = new();

    /// <summary>Throws <see cref="InvalidDataException"/> if any value is out of range.</summary>
    public void Validate()
    {
        Require(MaxBaseLevel >= 1, "maxBaseLevel must be at least 1.");
        Require(StartingStatValue >= 1, "startingStatValue must be at least 1.");
        Require(MaxStatValue >= StartingStatValue, "maxStatValue must be at least startingStatValue.");
        Require(StartingStatPoints >= 0, "startingStatPoints can't be negative.");
        Exp.Validate(MaxBaseLevel);
        StatPoints.Validate();
        Hp.Validate("hp");
        Sp.Validate("sp");
        AttackSpeed.Validate();
    }

    internal static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }
}

/// <summary>
/// EXP needed to go from level <c>L</c> to <c>L + 1</c>. Uses <see cref="Table"/> when it is set,
/// otherwise <c>round(Base + Factor * L ^ Exponent)</c>.
/// </summary>
public sealed record ExpCurve
{
    public double Base { get; init; } = 5;

    public double Factor { get; init; } = 4;

    public double Exponent { get; init; } = 2.2;

    /// <summary>
    /// Optional explicit values. Entry <c>i</c> is the EXP from level <c>i + 1</c> to <c>i + 2</c>,
    /// so it needs <c>maxBaseLevel - 1</c> entries.
    /// </summary>
    public IReadOnlyList<long>? Table { get; init; }

    /// <summary>EXP to go from <paramref name="level"/> to the next level.</summary>
    public long ExpToNext(int level)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        if (Table is not null)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(level, Table.Count);
            return Table[level - 1];
        }

        return Math.Max(1, (long)Math.Round(Base + Factor * Math.Pow(level, Exponent)));
    }

    internal void Validate(int maxBaseLevel)
    {
        if (Table is not null)
        {
            StatTable.Require(Table.Count >= maxBaseLevel - 1, $"exp.table needs {maxBaseLevel - 1} entries.");
            StatTable.Require(Table.All(v => v > 0), "exp.table values must be positive.");
            return;
        }

        StatTable.Require(Factor > 0 && Exponent > 0, "exp.factor and exp.exponent must be positive.");
        StatTable.Require(Base >= 0, "exp.base can't be negative.");
    }
}

/// <summary>Stat points gained on reaching level <c>L</c>: <c>Base + floor(L / BonusEvery)</c>.</summary>
public sealed record StatPointRules
{
    public int Base { get; init; } = 3;

    /// <summary>Every this many levels the reward grows by one. 0 turns the bonus off.</summary>
    public int BonusEvery { get; init; } = 5;

    public int PointsForReaching(int level) => Base + (BonusEvery > 0 ? level / BonusEvery : 0);

    internal void Validate()
    {
        StatTable.Require(Base >= 0, "statPoints.base can't be negative.");
        StatTable.Require(BonusEvery >= 0, "statPoints.bonusEvery can't be negative.");
    }
}

/// <summary>
/// Max HP or SP: <c>floor((Base + PerLevel * (level - 1)) * (1 + stat * PercentPerStat / 100))</c>,
/// where the stat is VIT for HP and INT for SP.
/// </summary>
public sealed record ResourceFormula
{
    public double Base { get; init; }

    public double PerLevel { get; init; }

    public double PercentPerStat { get; init; }

    public double Compute(int level, int stat) =>
        (Base + PerLevel * (level - 1)) * (1 + stat * PercentPerStat / 100.0);

    internal void Validate(string name)
    {
        StatTable.Require(Base > 0, $"{name}.base must be positive.");
        StatTable.Require(PerLevel >= 0 && PercentPerStat >= 0, $"{name} values can't be negative.");
    }
}

/// <summary>
/// Delay between attacks: <c>BaseDelay * (1 - min(MaxReduction, AGI * AgiReduction + DEX * DexReduction))</c>,
/// never below <see cref="MinDelay"/>. Attack speed is <c>1 / delay</c> attacks per second.
/// </summary>
public sealed record AttackSpeedFormula
{
    /// <summary>Seconds between attacks with 0 AGI and DEX.</summary>
    public double BaseDelay { get; init; } = 1.0;

    /// <summary>Fraction of the delay removed per AGI point.</summary>
    public double AgiReduction { get; init; } = 0.004;

    /// <summary>Fraction of the delay removed per DEX point.</summary>
    public double DexReduction { get; init; } = 0.001;

    /// <summary>Largest fraction of the delay stats can remove.</summary>
    public double MaxReduction { get; init; } = 0.6;

    /// <summary>Shortest delay, in seconds, after all modifiers.</summary>
    public double MinDelay { get; init; } = 0.2;

    public double Compute(int agi, int dex)
    {
        var reduction = Math.Min(MaxReduction, agi * AgiReduction + dex * DexReduction);
        return 1.0 / Math.Max(MinDelay, BaseDelay * (1 - reduction));
    }

    internal void Validate()
    {
        StatTable.Require(BaseDelay > 0 && MinDelay > 0, "attackSpeed.baseDelay and minDelay must be positive.");
        StatTable.Require(AgiReduction >= 0 && DexReduction >= 0, "attackSpeed reductions can't be negative.");
        StatTable.Require(MaxReduction is >= 0 and < 1, "attackSpeed.maxReduction must be in [0, 1).");
    }
}

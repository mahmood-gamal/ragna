using Ragna.Core.Combat;
using Ragna.Core.Items;

namespace Ragna.Core.Monsters;

/// <summary>
/// One monster type: stats, AI tuning and respawn time. Loaded from JSON with
/// <see cref="MonsterDatabase"/> (the game uses <c>data/monsters/monsters.json</c>).
/// Distances are in metres, times in seconds, speeds in metres per second.
/// </summary>
public sealed record MonsterDefinition
{
    /// <summary>Key that spawners and scenes use to pick this type, for example <c>slime</c>.</summary>
    public string Id { get; init; } = "";

    /// <summary>Name shown to the player.</summary>
    public string Name { get; init; } = "";

    public int Level { get; init; } = 1;

    public int MaxHp { get; init; } = 50;

    public int Atk { get; init; } = 5;

    public int Def { get; init; }

    public int Hit { get; init; } = 10;

    public int Flee { get; init; } = 2;

    /// <summary>EXP the killer gets.</summary>
    public int ExpReward { get; init; } = 10;

    /// <summary>Walking speed while wandering and going home.</summary>
    public float MoveSpeed { get; init; } = 1.5f;

    /// <summary>Running speed while chasing a target.</summary>
    public float ChaseSpeed { get; init; } = 3f;

    /// <summary>Seconds between two attacks.</summary>
    public double AttackDelay { get; init; } = 1.5;

    /// <summary>Seconds after dying before a new one spawns at the same spawn point.</summary>
    public double RespawnTime { get; init; } = 10;

    /// <summary>Sprite tint as <c>#rrggbb</c>, so placeholder monsters look different.</summary>
    public string Tint { get; init; } = "#ffffff";

    /// <summary>AI tuning: aggro, wander and leash.</summary>
    public MonsterBehavior Behavior { get; init; } = new();

    /// <summary>What it can drop when it dies. Each entry is rolled on its own (see <see cref="DropTable"/>).</summary>
    public IReadOnlyList<DropEntry> Drops { get; init; } = [];

    public CombatStats CombatStats => new(Level, Atk, Def, Hit, Flee, CritChance: 0);

    /// <summary>Throws <see cref="InvalidDataException"/> if any value is out of range.</summary>
    public void Validate()
    {
        Require(!string.IsNullOrWhiteSpace(Id), "id is required.");
        Require(Level >= 1, "level must be at least 1.");
        Require(MaxHp >= 1, "maxHp must be at least 1.");
        Require(Atk >= 0 && Def >= 0 && Hit >= 0 && Flee >= 0, "atk, def, hit and flee can't be negative.");
        Require(ExpReward >= 0, "expReward can't be negative.");
        Require(MoveSpeed > 0f && ChaseSpeed > 0f, "moveSpeed and chaseSpeed must be above 0.");
        Require(AttackDelay > 0, "attackDelay must be above 0.");
        Require(RespawnTime >= 0, "respawnTime can't be negative.");
        Require(TryParseTint(Tint, out _, out _, out _), "tint must look like #rrggbb.");
        var behavior = Behavior ?? throw new InvalidDataException($"Monster '{Id}': behavior can't be null.");
        var drops = Drops ?? throw new InvalidDataException($"Monster '{Id}': drops can't be null.");
        try
        {
            behavior.Validate();
            foreach (var drop in drops)
            {
                (drop ?? throw new ArgumentException("drops can't contain null.")).Validate();
            }
        }
        catch (ArgumentException e)
        {
            throw new InvalidDataException($"Monster '{Id}': {e.Message}", e);
        }
    }

    /// <summary>Reads <paramref name="hex"/> (<c>#rrggbb</c>) as red, green and blue from 0 to 1.</summary>
    public static bool TryParseTint(string hex, out float r, out float g, out float b)
    {
        r = g = b = 0f;
        if (hex is not { Length: 7 } || hex[0] != '#'
            || !int.TryParse(hex.AsSpan(1), System.Globalization.NumberStyles.HexNumber, null, out var rgb))
        {
            return false;
        }

        r = ((rgb >> 16) & 0xff) / 255f;
        g = ((rgb >> 8) & 0xff) / 255f;
        b = (rgb & 0xff) / 255f;
        return true;
    }

    private void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException($"Monster '{Id}': {message}");
        }
    }
}

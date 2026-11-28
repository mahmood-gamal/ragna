namespace Ragna.Core.Save;

/// <summary>
/// Everything a save file holds: the player's stats, position and inventory, and the dialogue
/// flags. Written and read as JSON by <see cref="SaveSerializer"/>.
/// </summary>
public sealed record SaveData
{
    /// <summary>The <see cref="Version"/> this build writes.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Save format version, so later builds can upgrade old saves.</summary>
    public int Version { get; init; } = CurrentVersion;

    /// <summary>When the game was saved (UTC).</summary>
    public DateTimeOffset SavedAt { get; init; }

    public PlayerSave Player { get; init; } = new();

    public InventorySave Inventory { get; init; } = new();

    /// <summary>Dialogue and story flags by name, see <see cref="Save.DialogueFlags"/>.</summary>
    public IReadOnlyDictionary<string, bool> DialogueFlags { get; init; } = new Dictionary<string, bool>();
}

/// <summary>The player's level, EXP, stats, HP, SP and where they stand.</summary>
public sealed record PlayerSave
{
    public int Level { get; init; } = 1;

    public long Exp { get; init; }

    public int StatPoints { get; init; }

    public int Str { get; init; } = 1;

    public int Agi { get; init; } = 1;

    public int Vit { get; init; } = 1;

    public int Int { get; init; } = 1;

    public int Dex { get; init; } = 1;

    public int Luk { get; init; } = 1;

    public int Hp { get; init; } = 1;

    public int Sp { get; init; }

    public PositionSave Position { get; init; } = new(0, 0, 0);
}

/// <summary>A point in the world, in metres.</summary>
public sealed record PositionSave(float X, float Y, float Z);

/// <summary>The inventory grid size and its non-empty slots.</summary>
public sealed record InventorySave
{
    public int Columns { get; init; }

    public int Rows { get; init; }

    /// <summary>Only the slots that hold something, in slot order.</summary>
    public IReadOnlyList<SlotSave> Slots { get; init; } = [];
}

/// <summary><paramref name="Quantity"/> of the item with id <paramref name="Item"/> in slot <paramref name="Slot"/>.</summary>
public sealed record SlotSave(int Slot, string Item, int Quantity);

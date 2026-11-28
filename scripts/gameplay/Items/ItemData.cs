using Godot;
using Ragna.Core.Items;
using Ragna.Gameplay.Monsters;

namespace Ragna.Gameplay.Items;

/// <summary>The game's item types from <c>data/items/items.json</c>, read once on first use.</summary>
public static class ItemData
{
    public const string Path = "res://data/items/items.json";

    private static ItemDatabase? _database;

    public static ItemDatabase Database => _database ??= Load();

    private static ItemDatabase Load()
    {
        if (!Godot.FileAccess.FileExists(Path))
        {
            GD.PushWarning($"{Path} not found; there are no item types.");
            return new ItemDatabase([]);
        }

        var database = ItemDatabase.FromJson(Godot.FileAccess.GetFileAsString(Path));
        try
        {
            database.CheckDrops(MonsterData.Database);
        }
        catch (InvalidDataException e)
        {
            // Unknown drops are skipped when they roll; say so once.
            GD.PushWarning(e.Message);
        }

        return database;
    }
}

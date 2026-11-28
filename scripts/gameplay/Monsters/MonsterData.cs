using Godot;
using Ragna.Core.Monsters;

namespace Ragna.Gameplay.Monsters;

/// <summary>The game's monster types from <c>data/monsters/monsters.json</c>, read once on first use.</summary>
public static class MonsterData
{
    public const string Path = "res://data/monsters/monsters.json";

    private static MonsterDatabase? _database;

    public static MonsterDatabase Database => _database ??= Load();

    /// <summary>The type with <paramref name="id"/>, or a default one (with a warning) if there is none.</summary>
    public static MonsterDefinition Get(string id)
    {
        if (Database.TryGet(id, out var monster))
        {
            return monster;
        }

        GD.PushWarning($"No monster '{id}' in {Path}; using default stats.");
        return new MonsterDefinition { Id = id, Name = id };
    }

    private static MonsterDatabase Load()
    {
        if (!Godot.FileAccess.FileExists(Path))
        {
            GD.PushWarning($"{Path} not found; there are no monster types.");
            return new MonsterDatabase([]);
        }

        return MonsterDatabase.FromJson(Godot.FileAccess.GetFileAsString(Path));
    }
}

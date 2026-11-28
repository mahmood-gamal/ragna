using Godot;
using Ragna.Core.Save;
using Ragna.Gameplay.Combat;
using Ragna.Gameplay.Items;

namespace Ragna.Gameplay.Save;

/// <summary>
/// Saves and loads the game to one local JSON file, <see cref="SavePath"/>. Registered as the
/// <c>SaveService</c> autoload, so any node can reach it at <c>/root/SaveService</c> or through
/// <see cref="Instance"/>. Press <b>F5</b> to save and <b>F9</b> to load.
/// </summary>
/// <remarks>
/// A save holds the player's stats, HP, SP and position (from the <see cref="PlayerCombat"/> in the
/// <see cref="PlayerCombat.Group"/> group), the player's inventory (the <see cref="PlayerInventory"/>
/// next to it) and <see cref="Flags"/>, the dialogue flags. The format lives in
/// <see cref="SaveData"/> and <see cref="SaveSerializer"/>. The file is written to a temporary file
/// first and then renamed, so a crash mid-save keeps the old save.
/// </remarks>
public partial class SaveService : Node
{
    public const string SavePath = "user://save.json";
    public const string SaveAction = "save_game";
    public const string LoadAction = "load_game";

    private const string TempPath = SavePath + ".tmp";

    /// <summary>The autoload, once it has entered the tree.</summary>
    public static SaveService? Instance { get; private set; }

    /// <summary>Raised after a save is written.</summary>
    public event Action? Saved;

    /// <summary>Raised after a save is loaded and applied.</summary>
    public event Action? Loaded;

    /// <summary>Raised when saving fails, with why.</summary>
    public event Action<string>? SaveFailed;

    /// <summary>Raised when loading fails, with why. Nothing was changed.</summary>
    public event Action<string>? LoadFailed;

    /// <summary>Dialogue and story flags. Saved and loaded with the game.</summary>
    public DialogueFlags Flags { get; } = new();

    /// <summary>Whether there is a save file to load.</summary>
    public static bool HasSave => Godot.FileAccess.FileExists(SavePath);

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(SaveAction))
        {
            Save();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed(LoadAction))
        {
            Load();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Writes the current game to <see cref="SavePath"/>.</summary>
    /// <returns>Whether it was saved. On failure <see cref="SaveFailed"/> says why.</returns>
    public bool Save()
    {
        if (FindPlayer() is not { } player)
        {
            return Fail(SaveFailed, "there is no player.");
        }

        var data = new SaveData
        {
            SavedAt = DateTimeOffset.UtcNow,
            Player = player.CaptureSave(),
            Inventory = FindInventory(player) is { } inventory
                ? SaveMapper.CaptureInventory(inventory.Inventory)
                : new InventorySave(),
            DialogueFlags = Flags.Snapshot(),
        };

        using (var file = Godot.FileAccess.Open(TempPath, Godot.FileAccess.ModeFlags.Write))
        {
            if (file is null)
            {
                return Fail(SaveFailed, $"can't write {TempPath} ({Godot.FileAccess.GetOpenError()}).");
            }

            file.StoreString(SaveSerializer.ToJson(data));
        }

        var renamed = DirAccess.RenameAbsolute(TempPath, SavePath);
        if (renamed != Error.Ok)
        {
            return Fail(SaveFailed, $"can't replace {SavePath} ({renamed}).");
        }

        GD.Print($"Saved to {ProjectSettings.GlobalizePath(SavePath)}.");
        Saved?.Invoke();
        return true;
    }

    /// <summary>Reads <see cref="SavePath"/> and applies it to the running game.</summary>
    /// <returns>Whether it was loaded. On failure nothing changes and <see cref="LoadFailed"/> says why.</returns>
    public bool Load()
    {
        if (!HasSave)
        {
            return Fail(LoadFailed, "no save file.");
        }

        if (FindPlayer() is not { } player)
        {
            return Fail(LoadFailed, "there is no player.");
        }

        SaveData data;
        try
        {
            data = SaveSerializer.FromJson(Godot.FileAccess.GetFileAsString(SavePath));
            player.RestoreSave(data.Player);
        }
        catch (InvalidDataException e)
        {
            return Fail(LoadFailed, e.Message);
        }

        if (FindInventory(player) is { } inventory)
        {
            foreach (var problem in SaveMapper.RestoreInventory(data.Inventory, inventory.Inventory, ItemData.Database))
            {
                GD.PushWarning($"Loading save: {problem}");
            }
        }

        Flags.Load(data.DialogueFlags);
        GD.Print($"Loaded save from {data.SavedAt.ToLocalTime():g}.");
        Loaded?.Invoke();
        return true;
    }

    private PlayerCombat? FindPlayer() => GetTree().GetFirstNodeInGroup(PlayerCombat.Group) as PlayerCombat;

    private static PlayerInventory? FindInventory(PlayerCombat player) =>
        player.Body.GetChildren().OfType<PlayerInventory>().FirstOrDefault();

    private static bool Fail(Action<string>? failed, string reason)
    {
        GD.PushWarning(reason);
        failed?.Invoke(reason);
        return false;
    }
}

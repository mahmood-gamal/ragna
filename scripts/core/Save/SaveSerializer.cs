using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ragna.Core.Save;

/// <summary>Turns <see cref="SaveData"/> into indented JSON and back.</summary>
public static class SaveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static string ToJson(SaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return JsonSerializer.Serialize(data, Options);
    }

    /// <summary>Reads a save written by <see cref="ToJson"/>.</summary>
    /// <exception cref="InvalidDataException">The text isn't a save, or it's from a newer build.</exception>
    public static SaveData FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        SaveData? data;
        try
        {
            data = JsonSerializer.Deserialize<SaveData>(json, Options);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Save file is not valid JSON: {e.Message}", e);
        }

        if (data is null)
        {
            throw new InvalidDataException("Save file is empty.");
        }

        if (data.Version < 1)
        {
            throw new InvalidDataException($"Save file has no valid version (got {data.Version}).");
        }

        if (data.Version > SaveData.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Save file is version {data.Version}, but this build only reads up to version {SaveData.CurrentVersion}.");
        }

        // A save with "player": null or "slots": null reads as defaults rather than crashing later.
        var player = data.Player ?? new PlayerSave();
        var inventory = data.Inventory ?? new InventorySave();
        return data with
        {
            Player = player with { Position = player.Position ?? new PositionSave(0, 0, 0) },
            Inventory = inventory with { Slots = (inventory.Slots ?? []).Where(s => s is not null).ToList() },
            DialogueFlags = data.DialogueFlags ?? new Dictionary<string, bool>(),
        };
    }
}

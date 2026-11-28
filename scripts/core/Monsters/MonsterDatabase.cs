using System.Text.Json;

namespace Ragna.Core.Monsters;

/// <summary>All monster types, by id. Read from JSON with <see cref="FromJson"/>.</summary>
public sealed class MonsterDatabase
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, MonsterDefinition> _byId;

    public MonsterDatabase(IEnumerable<MonsterDefinition> monsters)
    {
        ArgumentNullException.ThrowIfNull(monsters);
        _byId = new Dictionary<string, MonsterDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var monster in monsters)
        {
            monster.Validate();
            if (!_byId.TryAdd(monster.Id, monster))
            {
                throw new InvalidDataException($"Monster id '{monster.Id}' is used twice.");
            }
        }
    }

    public IReadOnlyCollection<MonsterDefinition> All => _byId.Values;

    /// <summary>The monster type with <paramref name="id"/> (case-insensitive).</summary>
    /// <exception cref="KeyNotFoundException">No monster has that id.</exception>
    public MonsterDefinition Get(string id) =>
        TryGet(id, out var monster) ? monster : throw new KeyNotFoundException($"No monster with id '{id}'.");

    public bool TryGet(string id, out MonsterDefinition monster) => _byId.TryGetValue(id, out monster!);

    /// <summary>
    /// Parses and validates <paramref name="json"/>: <c>{ "monsters": [ { "id": "slime", ... } ] }</c>.
    /// Property names are camelCase. Missing values keep their defaults.
    /// </summary>
    /// <exception cref="InvalidDataException">The JSON is malformed, a value is out of range or an id repeats.</exception>
    public static MonsterDatabase FromJson(string json)
    {
        MonsterFile? file;
        try
        {
            file = JsonSerializer.Deserialize<MonsterFile>(json, Options);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Invalid monster JSON: {e.Message}", e);
        }

        if (file?.Monsters is null)
        {
            throw new InvalidDataException("Monster JSON needs a \"monsters\" array.");
        }

        return new MonsterDatabase(file.Monsters);
    }

    private sealed record MonsterFile(List<MonsterDefinition>? Monsters);
}

using System.Text.Json;
using Ragna.Core.Monsters;

namespace Ragna.Core.Items;

/// <summary>All item types, by id. Read from JSON with <see cref="FromJson"/>.</summary>
public sealed class ItemDatabase
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, ItemDefinition> _byId;

    public ItemDatabase(IEnumerable<ItemDefinition> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _byId = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            item.Validate();
            if (!_byId.TryAdd(item.Id, item))
            {
                throw new InvalidDataException($"Item id '{item.Id}' is used twice.");
            }
        }
    }

    public IReadOnlyCollection<ItemDefinition> All => _byId.Values;

    /// <summary>The item type with <paramref name="id"/> (case-insensitive).</summary>
    /// <exception cref="KeyNotFoundException">No item has that id.</exception>
    public ItemDefinition Get(string id) =>
        TryGet(id, out var item) ? item : throw new KeyNotFoundException($"No item with id '{id}'.");

    public bool TryGet(string id, out ItemDefinition item) => _byId.TryGetValue(id, out item!);

    /// <summary>
    /// Throws <see cref="InvalidDataException"/> if a drop table in <paramref name="monsters"/>
    /// names an item that isn't in this database.
    /// </summary>
    public void CheckDrops(MonsterDatabase monsters)
    {
        ArgumentNullException.ThrowIfNull(monsters);
        foreach (var monster in monsters.All)
        {
            foreach (var drop in monster.Drops)
            {
                if (!_byId.ContainsKey(drop.Item))
                {
                    throw new InvalidDataException($"Monster '{monster.Id}' drops unknown item '{drop.Item}'.");
                }
            }
        }
    }

    /// <summary>
    /// Parses and validates <paramref name="json"/>: <c>{ "items": [ { "id": "slime_gel", ... } ] }</c>.
    /// Property names are camelCase. Missing values keep their defaults.
    /// </summary>
    /// <exception cref="InvalidDataException">The JSON is malformed, a value is out of range or an id repeats.</exception>
    public static ItemDatabase FromJson(string json)
    {
        ItemFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ItemFile>(json, Options);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Invalid item JSON: {e.Message}", e);
        }

        if (file?.Items is null)
        {
            throw new InvalidDataException("Item JSON needs an \"items\" array.");
        }

        return new ItemDatabase(file.Items);
    }

    private sealed record ItemFile(List<ItemDefinition>? Items);
}

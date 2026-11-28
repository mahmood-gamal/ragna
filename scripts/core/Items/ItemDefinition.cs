using Ragna.Core.Monsters;

namespace Ragna.Core.Items;

/// <summary>
/// One item type. Loaded from JSON with <see cref="ItemDatabase"/> (the game uses
/// <c>data/items/items.json</c>).
/// </summary>
public sealed record ItemDefinition
{
    /// <summary>Key that drop tables and saves use, for example <c>slime_gel</c>.</summary>
    public string Id { get; init; } = "";

    /// <summary>Name shown to the player.</summary>
    public string Name { get; init; } = "";

    /// <summary>One line shown in tooltips.</summary>
    public string Description { get; init; } = "";

    /// <summary>Colour of the placeholder ground sprite as <c>#rrggbb</c>.</summary>
    public string Tint { get; init; } = "#ffffff";

    /// <summary>Throws <see cref="InvalidDataException"/> if any value is out of range.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidDataException("Item: id is required.");
        }

        if (!MonsterDefinition.TryParseTint(Tint, out _, out _, out _))
        {
            throw new InvalidDataException($"Item '{Id}': tint must look like #rrggbb.");
        }
    }
}

using System.Text.Json;

namespace Ragna.Core.Stats;

/// <summary>Reads a <see cref="StatTable"/> from JSON. Property names are camelCase.</summary>
public static class StatTableLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Parses and validates <paramref name="json"/>. Missing values keep their defaults.</summary>
    /// <exception cref="InvalidDataException">The JSON is malformed or a value is out of range.</exception>
    public static StatTable FromJson(string json)
    {
        StatTable? table;
        try
        {
            table = JsonSerializer.Deserialize<StatTable>(json, Options);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Invalid stat table JSON: {e.Message}", e);
        }

        if (table is null)
        {
            throw new InvalidDataException("Stat table JSON is null.");
        }

        table.Validate();
        return table;
    }
}

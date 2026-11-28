namespace Ragna.Core.Save;

/// <summary>
/// Named true/false flags that dialogue and story triggers set and check, for example
/// <c>met_elder</c>. Flags never set read as false. Saved with the game.
/// </summary>
/// <remarks>Names match exactly (case-sensitive).</remarks>
public sealed class DialogueFlags
{
    private readonly Dictionary<string, bool> _flags = new(StringComparer.Ordinal);

    /// <summary>Raised after a flag changes, with its name and new value.</summary>
    public event Action<string, bool>? Changed;

    /// <summary>Every flag that has been set, true or false.</summary>
    public IReadOnlyDictionary<string, bool> All => _flags;

    /// <summary>The flag's value, or false if it was never set.</summary>
    public bool Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _flags.TryGetValue(name, out var value) && value;
    }

    /// <summary>Whether the flag was ever set, to true or false.</summary>
    public bool Has(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _flags.ContainsKey(name);
    }

    public void Set(string name, bool value = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_flags.TryGetValue(name, out var old) && old == value)
        {
            return;
        }

        _flags[name] = value;
        Changed?.Invoke(name, value);
    }

    /// <summary>Forgets every flag.</summary>
    public void Clear() => _flags.Clear();

    /// <summary>A copy of the flags, sorted by name, for saving.</summary>
    public IReadOnlyDictionary<string, bool> Snapshot() => new SortedDictionary<string, bool>(_flags, StringComparer.Ordinal);

    /// <summary>Replaces every flag with <paramref name="flags"/>, as when loading a save. Blank names are skipped.</summary>
    public void Load(IReadOnlyDictionary<string, bool> flags)
    {
        ArgumentNullException.ThrowIfNull(flags);
        _flags.Clear();
        foreach (var (name, value) in flags)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _flags[name] = value;
            }
        }
    }
}

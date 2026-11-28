namespace Ragna.Core.Dialogue;

/// <summary>Every line shown and choice picked in a dialogue scene, oldest first, for the history log.</summary>
public sealed class DialogueHistory
{
    private readonly List<HistoryEntry> _entries = [];

    /// <summary>Raised after an entry is added.</summary>
    public event Action<HistoryEntry>? Added;

    public IReadOnlyList<HistoryEntry> Entries => _entries;

    public void AddLine(string? speaker, string text) => Add(new HistoryEntry(speaker, text, IsChoice: false));

    public void AddChoice(string text) => Add(new HistoryEntry(null, text, IsChoice: true));

    private void Add(HistoryEntry entry)
    {
        _entries.Add(entry);
        Added?.Invoke(entry);
    }
}

/// <summary>A line (<paramref name="Speaker"/> is null for narration) or, when <paramref name="IsChoice"/>, the option the player picked.</summary>
public sealed record HistoryEntry(string? Speaker, string Text, bool IsChoice);

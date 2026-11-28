namespace Ragna.Core.Dialogue;

/// <summary>Reveals a line of text a few characters at a time.</summary>
public sealed class Typewriter
{
    private double _shown;

    /// <param name="charactersPerSecond">How fast text appears. 0 or less shows each line at once.</param>
    public Typewriter(double charactersPerSecond = 40)
    {
        CharactersPerSecond = charactersPerSecond;
    }

    /// <summary>How fast text appears. 0 or less shows each line at once.</summary>
    public double CharactersPerSecond { get; set; }

    public string Text { get; private set; } = "";

    /// <summary>How many characters of <see cref="Text"/> are showing.</summary>
    public int VisibleCharacters => (int)Math.Min(Math.Floor(_shown), Text.Length);

    public bool IsComplete => VisibleCharacters >= Text.Length;

    /// <summary>Starts revealing <paramref name="text"/> from its first character.</summary>
    public void Start(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        _shown = CharactersPerSecond > 0 ? 0 : text.Length;
    }

    /// <summary>Reveals the characters due after <paramref name="seconds"/> more.</summary>
    public void Update(double seconds)
    {
        if (IsComplete || seconds <= 0)
        {
            return;
        }

        _shown = CharactersPerSecond > 0 ? Math.Min(_shown + (seconds * CharactersPerSecond), Text.Length) : Text.Length;
    }

    /// <summary>Shows the whole line at once.</summary>
    public void Complete() => _shown = Text.Length;
}

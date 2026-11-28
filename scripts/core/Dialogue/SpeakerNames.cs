namespace Ragna.Core.Dialogue;

/// <summary>How speaker ids show on the name plate.</summary>
public static class SpeakerNames
{
    private static readonly char[] Separators = ['_', '-', ' '];

    /// <summary>Turns a character id into a name for the name plate: <c>old_man</c> becomes <c>Old Man</c>.</summary>
    public static string Display(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var words = id.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }
}

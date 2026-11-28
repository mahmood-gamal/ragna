namespace Ragna.Core.Dialogue;

/// <summary>The portrait expressions a script may use. Each character has one portrait per expression.</summary>
public static class Expressions
{
    public const string Neutral = "neutral";
    public const string Happy = "happy";
    public const string Angry = "angry";
    public const string Sad = "sad";
    public const string Surprised = "surprised";

    /// <summary>Every known expression, <see cref="Neutral"/> first.</summary>
    public static IReadOnlyList<string> All { get; } = [Neutral, Happy, Angry, Sad, Surprised];

    public static bool IsKnown(string expression) => All.Contains(expression, StringComparer.Ordinal);
}

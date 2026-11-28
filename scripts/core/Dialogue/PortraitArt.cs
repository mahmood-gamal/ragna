using System.Text.RegularExpressions;

namespace Ragna.Core.Dialogue;

/// <summary>
/// The rules for dialogue bust portraits: where the files go, their size and how they line up on the stage.
/// See docs/dialogue.md.
/// </summary>
/// <remarks>
/// <para>Files: <c>assets/portraits/&lt;character&gt;/&lt;expression&gt;.png</c>, one per <see cref="Expressions"/>
/// name. <c>neutral.png</c> is required; a missing expression falls back to it.</para>
/// <para>Size: <see cref="Width"/> x <see cref="Height"/> px, transparent background, facing right.</para>
/// <para>Anchor: bottom centre. The bottom pixel row stands on the bottom of the screen, centred on the slot.</para>
/// </remarks>
public static partial class PortraitArt
{
    /// <summary>Folder that holds one folder of portraits per character, relative to the project.</summary>
    public const string Root = "assets/portraits";

    public const int Width = 300;

    public const int Height = 440;

    /// <summary>The file for <paramref name="character"/> with <paramref name="expression"/>, relative to the project.</summary>
    public static string PathFor(string character, string expression)
    {
        if (!IsValidCharacterId(character))
        {
            throw new ArgumentException($"'{character}' is not a valid character id (lower case letters, digits and _).", nameof(character));
        }

        if (!Expressions.IsKnown(expression))
        {
            throw new ArgumentException($"'{expression}' is not a known expression.", nameof(expression));
        }

        return $"{Root}/{character}/{expression}.png";
    }

    /// <summary>Whether <paramref name="character"/> is a valid character id and folder name, such as <c>old_man</c>.</summary>
    public static bool IsValidCharacterId(string? character) => character is not null && CharacterId().IsMatch(character);

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex CharacterId();
}

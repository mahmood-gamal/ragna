using Ragna.Core.Save;

namespace Ragna.Core.Dialogue;

/// <summary>
/// When a story trigger plays its dialogue. A trigger can play once (remembered in a dialogue flag, so it stays
/// played after a save and load) and can wait for another flag, for example to play only after the player met an NPC.
/// </summary>
/// <remarks>
/// The engine node calls <see cref="ShouldPlay"/> when the player walks in, and <see cref="MarkPlayed"/> once the
/// dialogue has started.
/// </remarks>
public sealed record StoryTriggerRule
{
    /// <summary>Prefix of the flag a play-once trigger sets when <see cref="OnceFlag"/> is left empty.</summary>
    public const string DefaultOnceFlagPrefix = "trigger.";

    /// <param name="dialogueId">The script to play, <c>data/dialogue/&lt;dialogueId&gt;.json</c>.</param>
    /// <param name="once">Whether the trigger plays only once.</param>
    /// <param name="onceFlag">Flag that remembers a play-once trigger has played. Empty uses <c>trigger.&lt;dialogueId&gt;</c>.</param>
    /// <param name="requiredFlag">Flag that must equal <paramref name="requiredValue"/> for the trigger to play. Empty means none.</param>
    /// <param name="requiredValue">Value <paramref name="requiredFlag"/> must have.</param>
    public StoryTriggerRule(string dialogueId, bool once = true, string? onceFlag = null, string? requiredFlag = null, bool requiredValue = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dialogueId);
        DialogueId = dialogueId;
        Once = once;
        OnceFlag = string.IsNullOrWhiteSpace(onceFlag) ? DefaultOnceFlagPrefix + dialogueId : onceFlag;
        RequiredFlag = string.IsNullOrWhiteSpace(requiredFlag) ? null : requiredFlag;
        RequiredValue = requiredValue;
    }

    public string DialogueId { get; }

    public bool Once { get; }

    /// <summary>The flag that remembers a play-once trigger has played. Only used when <see cref="Once"/> is true.</summary>
    public string OnceFlag { get; }

    public string? RequiredFlag { get; }

    public bool RequiredValue { get; }

    /// <summary>Whether the trigger should play now, given <paramref name="flags"/>.</summary>
    public bool ShouldPlay(DialogueFlags flags)
    {
        ArgumentNullException.ThrowIfNull(flags);
        if (Once && flags.Get(OnceFlag))
        {
            return false;
        }

        return RequiredFlag is null || flags.Get(RequiredFlag) == RequiredValue;
    }

    /// <summary>Records that the trigger played. A play-once trigger sets <see cref="OnceFlag"/>.</summary>
    public void MarkPlayed(DialogueFlags flags)
    {
        ArgumentNullException.ThrowIfNull(flags);
        if (Once)
        {
            flags.Set(OnceFlag);
        }
    }
}

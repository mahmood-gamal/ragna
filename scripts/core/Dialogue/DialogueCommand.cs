namespace Ragna.Core.Dialogue;

/// <summary>One step of a <see cref="DialogueScript"/>. Read from JSON by <see cref="DialogueScript.FromJson"/>.</summary>
public abstract record DialogueCommand;

/// <summary>
/// Puts <paramref name="Character"/> on stage in <paramref name="Slot"/>, showing <paramref name="Expression"/>.
/// <paramref name="Flip"/> mirrors the portrait from the way it faces by default (see <see cref="StageSlots.FacesLeft"/>).
/// </summary>
public sealed record EnterCommand(string Character, StageSlot Slot, string Expression, bool Flip = false) : DialogueCommand;

/// <summary>Takes <paramref name="Character"/> off stage, or everyone when it is null.</summary>
public sealed record ExitCommand(string? Character) : DialogueCommand;

/// <summary>Changes the portrait of a character who is on stage.</summary>
public sealed record ExpressionCommand(string Character, string Expression) : DialogueCommand;

/// <summary>A line of text. <paramref name="Speaker"/> is null for narration.</summary>
public sealed record SayCommand(string? Speaker, string Text) : DialogueCommand;

/// <summary>Asks the player to pick one of <paramref name="Options"/>.</summary>
public sealed record ChoiceCommand(IReadOnlyList<ChoiceOption> Options) : DialogueCommand;

/// <summary>
/// A choice the player can pick. Runs <paramref name="Then"/>, then carries on after the choice.
/// When <paramref name="IfFlag"/> is set, the option only shows if that flag equals <paramref name="IfValue"/>.
/// </summary>
public sealed record ChoiceOption(string Text, IReadOnlyList<DialogueCommand> Then, string? IfFlag = null, bool IfValue = true);

/// <summary>Sets a dialogue flag.</summary>
public sealed record SetFlagCommand(string Flag, bool Value) : DialogueCommand;

/// <summary>Runs <paramref name="Then"/> if the flag equals <paramref name="Value"/>, otherwise <paramref name="Else"/>.</summary>
public sealed record IfFlagCommand(string Flag, bool Value, IReadOnlyList<DialogueCommand> Then, IReadOnlyList<DialogueCommand> Else) : DialogueCommand;

/// <summary>Changes the background image. <paramref name="Image"/> is a resource path or id, or null to clear it.</summary>
public sealed record BackgroundCommand(string? Image) : DialogueCommand;

/// <summary>Pauses for <paramref name="Seconds"/>.</summary>
public sealed record WaitCommand(double Seconds) : DialogueCommand;

/// <summary>Ends the script at once.</summary>
public sealed record EndCommand : DialogueCommand;

/// <summary>Where a character stands on the dialogue stage, from the left edge of the screen to the right.</summary>
/// <remarks>Most scenes use <see cref="Left"/> and <see cref="Right"/>. <see cref="FarLeft"/> and <see cref="FarRight"/> make room for group scenes of four.</remarks>
public enum StageSlot
{
    FarLeft,
    Left,
    Center,
    Right,
    FarRight,
}

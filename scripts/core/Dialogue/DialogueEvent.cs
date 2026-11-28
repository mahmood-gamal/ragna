namespace Ragna.Core.Dialogue;

/// <summary>
/// What the dialogue UI should do next, returned by <see cref="DialogueRunner.Advance"/>.
/// Flags and branches are handled by the runner and never reach the UI.
/// </summary>
public abstract record DialogueEvent
{
    /// <summary>
    /// True when the UI should call <see cref="DialogueRunner.Advance"/> again straight away,
    /// false when it should wait (for a click, a timer or a choice) or the script is over.
    /// </summary>
    public virtual bool Instant => true;
}

public sealed record CharacterEntered(string Character, StageSlot Slot, string Expression) : DialogueEvent;

public sealed record CharacterExited(string Character, StageSlot Slot) : DialogueEvent;

public sealed record ExpressionChanged(string Character, StageSlot Slot, string Expression) : DialogueEvent;

public sealed record BackgroundChanged(string? Image) : DialogueEvent;

/// <summary>Show the line, then call <see cref="DialogueRunner.Advance"/> when the player clicks.</summary>
public sealed record LineShown(string? Speaker, string Text) : DialogueEvent
{
    public override bool Instant => false;
}

/// <summary>Wait <see cref="Seconds"/>, then call <see cref="DialogueRunner.Advance"/>.</summary>
public sealed record WaitRequested(double Seconds) : DialogueEvent
{
    public override bool Instant => false;
}

/// <summary>Show <see cref="Options"/>, then call <see cref="DialogueRunner.Choose"/> with the picked index.</summary>
public sealed record ChoiceShown(IReadOnlyList<string> Options) : DialogueEvent
{
    public override bool Instant => false;
}

/// <summary>The script is over. Further calls to <see cref="DialogueRunner.Advance"/> keep returning this.</summary>
public sealed record DialogueEnded : DialogueEvent
{
    public override bool Instant => false;
}

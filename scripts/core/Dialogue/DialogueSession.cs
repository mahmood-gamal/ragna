namespace Ragna.Core.Dialogue;

/// <summary>
/// Drives a <see cref="DialogueRunner"/> for the dialogue scene UI: types lines out with a
/// <see cref="Typewriter"/>, counts down waits, holds the shown choices and keeps a <see cref="DialogueHistory"/>.
/// The UI forwards input (<see cref="Confirm"/>, <see cref="Choose"/>, the skip key in <see cref="Update"/>)
/// and draws what the session's events and properties say.
/// </summary>
public sealed class DialogueSession
{
    private double _waitLeft;

    public DialogueSession(DialogueRunner runner, double charactersPerSecond = 40)
    {
        ArgumentNullException.ThrowIfNull(runner);
        Runner = runner;
        Typewriter = new Typewriter(charactersPerSecond);
    }

    /// <summary>
    /// Raised for each change the stage needs to show: <see cref="CharacterEntered"/>, <see cref="CharacterExited"/>,
    /// <see cref="ExpressionChanged"/> and <see cref="BackgroundChanged"/>.
    /// </summary>
    public event Action<DialogueEvent>? StageChanged;

    /// <summary>Raised when a new line starts typing. Read <see cref="Speaker"/> and <see cref="Text"/>.</summary>
    public event Action? LineStarted;

    /// <summary>Raised when a choice is shown. Read <see cref="Options"/>.</summary>
    public event Action? ChoiceShown;

    /// <summary>Raised once, when the script is over.</summary>
    public event Action? Ended;

    public DialogueRunner Runner { get; }

    public Typewriter Typewriter { get; }

    public DialogueHistory History { get; } = new();

    public DialogueSessionState State { get; private set; } = DialogueSessionState.NotStarted;

    /// <summary>Who says the current line, or null for narration.</summary>
    public string? Speaker { get; private set; }

    /// <summary>The whole current line. <see cref="Typewriter"/> says how much of it shows.</summary>
    public string Text => Typewriter.Text;

    /// <summary>The options of the shown choice, empty when no choice is shown.</summary>
    public IReadOnlyList<string> Options { get; private set; } = [];

    /// <summary>Runs the script up to its first line, wait or choice.</summary>
    /// <exception cref="InvalidOperationException">The session has already started.</exception>
    public void Start()
    {
        if (State != DialogueSessionState.NotStarted)
        {
            throw new InvalidOperationException("The dialogue session has already started.");
        }

        Pump();
    }

    /// <summary>
    /// The player clicked or pressed Enter: a line still typing shows in full; a full line moves on.
    /// Does nothing during a wait or a choice.
    /// </summary>
    public void Confirm()
    {
        switch (State)
        {
            case DialogueSessionState.Typing:
                Typewriter.Complete();
                State = DialogueSessionState.LineComplete;
                break;
            case DialogueSessionState.LineComplete:
                Pump();
                break;
        }
    }

    /// <summary>
    /// Moves time on by <paramref name="seconds"/>. While <paramref name="skip"/> is held, each call finishes
    /// the current line or wait and moves on, stopping at choices.
    /// </summary>
    public void Update(double seconds, bool skip = false)
    {
        switch (State)
        {
            case DialogueSessionState.Typing when skip:
            case DialogueSessionState.LineComplete when skip:
                Typewriter.Complete();
                Pump();
                break;
            case DialogueSessionState.Typing:
                Typewriter.Update(seconds);
                if (Typewriter.IsComplete)
                {
                    State = DialogueSessionState.LineComplete;
                }

                break;
            case DialogueSessionState.Waiting:
                _waitLeft -= Math.Max(seconds, 0);
                if (skip || _waitLeft <= 0)
                {
                    Pump();
                }

                break;
        }
    }

    /// <summary>Picks option <paramref name="index"/> of <see cref="Options"/> and moves on.</summary>
    /// <exception cref="InvalidOperationException">No choice is shown.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not one of <see cref="Options"/>.</exception>
    public void Choose(int index)
    {
        if (State != DialogueSessionState.Choosing)
        {
            throw new InvalidOperationException("No choice is shown.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Options.Count);
        var picked = Options[index];
        Runner.Choose(index);
        Options = [];
        History.AddChoice(picked);
        Pump();
    }

    private void Pump()
    {
        while (true)
        {
            var next = Runner.Advance();
            switch (next)
            {
                case CharacterEntered or CharacterExited or ExpressionChanged or BackgroundChanged:
                    StageChanged?.Invoke(next);
                    continue;
                case LineShown line:
                    Speaker = line.Speaker;
                    Typewriter.Start(line.Text);
                    History.AddLine(line.Speaker, line.Text);
                    State = Typewriter.IsComplete ? DialogueSessionState.LineComplete : DialogueSessionState.Typing;
                    LineStarted?.Invoke();
                    return;
                case WaitRequested wait:
                    _waitLeft = wait.Seconds;
                    State = DialogueSessionState.Waiting;
                    return;
                case Dialogue.ChoiceShown choice:
                    Options = choice.Options;
                    State = DialogueSessionState.Choosing;
                    ChoiceShown?.Invoke();
                    return;
                case DialogueEnded:
                    if (State != DialogueSessionState.Ended)
                    {
                        State = DialogueSessionState.Ended;
                        Ended?.Invoke();
                    }

                    return;
                default:
                    throw new InvalidOperationException($"Unknown dialogue event {next.GetType().Name}.");
            }
        }
    }
}

/// <summary>What a <see cref="DialogueSession"/> is waiting for.</summary>
public enum DialogueSessionState
{
    NotStarted,

    /// <summary>A line is typing out. <see cref="DialogueSession.Confirm"/> shows it in full.</summary>
    Typing,

    /// <summary>A line shows in full. <see cref="DialogueSession.Confirm"/> moves on.</summary>
    LineComplete,

    /// <summary>A <c>wait</c> is counting down.</summary>
    Waiting,

    /// <summary>A choice is shown. Call <see cref="DialogueSession.Choose"/>.</summary>
    Choosing,

    Ended,
}

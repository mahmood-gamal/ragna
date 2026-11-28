using Ragna.Core.Save;

namespace Ragna.Core.Dialogue;

/// <summary>
/// Steps through a <see cref="DialogueScript"/>. Call <see cref="Advance"/> to get the next
/// <see cref="DialogueEvent"/> for the UI. Flags and branches are handled here and never reach the UI.
/// </summary>
/// <remarks>
/// A typical UI loop: call <see cref="Advance"/> while the event is <see cref="DialogueEvent.Instant"/>, applying each one.
/// On <see cref="LineShown"/> wait for a click, on <see cref="WaitRequested"/> wait the time, on
/// <see cref="ChoiceShown"/> call <see cref="Choose"/>, and on <see cref="DialogueEnded"/> close the scene.
/// </remarks>
public sealed class DialogueRunner
{
    /// <summary>The expression a character enters with when the script doesn't name one.</summary>
    public const string DefaultExpression = "neutral";

    private readonly DialogueFlags _flags;
    private readonly Stack<Frame> _frames = new();
    private readonly Queue<DialogueEvent> _pending = new();
    private readonly Dictionary<StageSlot, StageActor> _stage = [];
    private IReadOnlyList<ChoiceOption>? _visibleOptions;

    public DialogueRunner(DialogueScript script, DialogueFlags flags)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(flags);
        Script = script;
        _flags = flags;
        _frames.Push(new Frame(script.Commands));
    }

    public DialogueScript Script { get; }

    /// <summary>True while a choice is shown and <see cref="Choose"/> must be called before <see cref="Advance"/>.</summary>
    public bool AwaitingChoice => _visibleOptions is not null;

    public bool Finished { get; private set; }

    /// <summary>Who is on stage, by slot.</summary>
    public IReadOnlyDictionary<StageSlot, StageActor> Stage => _stage;

    /// <summary>The current background, or null for none.</summary>
    public string? Background { get; private set; }

    /// <summary>Runs the script up to the next thing the UI must show and returns it.</summary>
    /// <exception cref="InvalidOperationException">A choice is waiting for <see cref="Choose"/>.</exception>
    /// <exception cref="InvalidDataException">The script changes the expression of, or removes, a character who isn't on stage.</exception>
    public DialogueEvent Advance()
    {
        if (AwaitingChoice)
        {
            throw new InvalidOperationException("Call Choose before Advance: a choice is waiting.");
        }

        while (_pending.Count == 0)
        {
            if (Finished)
            {
                return new DialogueEnded();
            }

            var command = NextCommand();
            if (command is null)
            {
                Finish();
                return new DialogueEnded();
            }

            Run(command);
        }

        return _pending.Dequeue();
    }

    /// <summary>Picks option <paramref name="index"/> of the shown choice (an index into <see cref="ChoiceShown.Options"/>).</summary>
    /// <exception cref="InvalidOperationException">No choice is shown.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not one of the shown options.</exception>
    public void Choose(int index)
    {
        var options = _visibleOptions ?? throw new InvalidOperationException("No choice is waiting.");
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, options.Count);
        _visibleOptions = null;
        _frames.Push(new Frame(options[index].Then));
    }

    private DialogueCommand? NextCommand()
    {
        while (_frames.Count > 0)
        {
            var frame = _frames.Peek();
            if (frame.Index < frame.Commands.Count)
            {
                return frame.Commands[frame.Index++];
            }

            _frames.Pop();
        }

        return null;
    }

    private void Run(DialogueCommand command)
    {
        switch (command)
        {
            case EnterCommand enter:
                Enter(enter);
                break;
            case ExitCommand exit:
                Exit(exit.Character);
                break;
            case ExpressionCommand expression:
                var slot = SlotOf(expression.Character)
                    ?? throw new InvalidDataException(
                        $"Dialogue '{Script.Id}': expression for '{expression.Character}', who is not on stage.");
                _stage[slot] = _stage[slot] with { Expression = expression.Expression };
                _pending.Enqueue(new ExpressionChanged(expression.Character, slot, expression.Expression));
                break;
            case SayCommand say:
                _pending.Enqueue(new LineShown(say.Speaker, say.Text));
                break;
            case ChoiceCommand choice:
                var visible = choice.Options.Where(IsVisible).ToList();
                if (visible.Count > 0)
                {
                    _visibleOptions = visible;
                    _pending.Enqueue(new ChoiceShown(visible.Select(o => o.Text).ToList()));
                }

                break;
            case SetFlagCommand set:
                _flags.Set(set.Flag, set.Value);
                break;
            case IfFlagCommand branch:
                _frames.Push(new Frame(_flags.Get(branch.Flag) == branch.Value ? branch.Then : branch.Else));
                break;
            case BackgroundCommand background:
                Background = background.Image;
                _pending.Enqueue(new BackgroundChanged(background.Image));
                break;
            case WaitCommand wait:
                _pending.Enqueue(new WaitRequested(wait.Seconds));
                break;
            case EndCommand:
                Finish();
                _pending.Enqueue(new DialogueEnded());
                break;
            default:
                throw new InvalidOperationException($"Unknown dialogue command {command.GetType().Name}.");
        }
    }

    private bool IsVisible(ChoiceOption option) => option.IfFlag is null || _flags.Get(option.IfFlag) == option.IfValue;

    private void Enter(EnterCommand enter)
    {
        // A character already on stage moves; whoever stood in the target slot leaves.
        var current = SlotOf(enter.Character);
        if (current is { } from && from != enter.Slot)
        {
            _stage.Remove(from);
            _pending.Enqueue(new CharacterExited(enter.Character, from));
        }

        if (_stage.TryGetValue(enter.Slot, out var occupant) && occupant.Character != enter.Character)
        {
            _pending.Enqueue(new CharacterExited(occupant.Character, enter.Slot));
        }

        _stage[enter.Slot] = new StageActor(enter.Character, enter.Expression);
        _pending.Enqueue(new CharacterEntered(enter.Character, enter.Slot, enter.Expression));
    }

    private void Exit(string? character)
    {
        if (character is null)
        {
            foreach (var (slot, actor) in _stage.OrderBy(pair => pair.Key))
            {
                _pending.Enqueue(new CharacterExited(actor.Character, slot));
            }

            _stage.Clear();
            return;
        }

        var at = SlotOf(character)
            ?? throw new InvalidDataException($"Dialogue '{Script.Id}': exit for '{character}', who is not on stage.");
        _stage.Remove(at);
        _pending.Enqueue(new CharacterExited(character, at));
    }

    private StageSlot? SlotOf(string character)
    {
        foreach (var (slot, actor) in _stage)
        {
            if (actor.Character == character)
            {
                return slot;
            }
        }

        return null;
    }

    private void Finish()
    {
        Finished = true;
        _frames.Clear();
    }

    private sealed class Frame(IReadOnlyList<DialogueCommand> commands)
    {
        public IReadOnlyList<DialogueCommand> Commands { get; } = commands;

        public int Index { get; set; }
    }
}

/// <summary>A character on the dialogue stage and the portrait they show.</summary>
public sealed record StageActor(string Character, string Expression);

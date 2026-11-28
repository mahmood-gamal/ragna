using Godot;
using Ragna.Core.Dialogue;
using Ragna.Core.Save;
using Ragna.Gameplay.Save;

namespace Ragna.Dialogue;

/// <summary>
/// The full-screen dialogue scene: bust portraits on a stage, a text box with a speaker name plate and a
/// typewriter, choices and a history log, over a dimmed, blurred world or a full-screen image. The world pauses
/// while it plays. Start one with <see cref="Play(string)"/>; <see cref="Finished"/> says when it is over.
/// </summary>
/// <remarks>
/// <para>The script logic lives in <see cref="DialogueSession"/>; this node draws it and forwards input.</para>
/// <para>Controls: click or <b>Enter</b> shows the whole line, then moves on. Hold <b>Ctrl</b> to skip lines up to
/// the next choice. <b>1</b>-<b>3</b> or a click picks a choice. <b>H</b> or the Log button opens the history log.</para>
/// <para>Backgrounds: a script's <c>background</c> with an <c>image</c> shows that image full-screen. The image is a
/// resource path (<c>res://...</c>) or a name in <c>res://assets/backgrounds/</c> (<c>village</c> is
/// <c>village.png</c>). No background, or one that can't be found, shows the world dimmed and blurred.</para>
/// </remarks>
public partial class DialogueScene : CanvasLayer
{
    public const string DialogueRoot = "res://data/dialogue";
    public const string BackgroundRoot = "res://assets/backgrounds";
    public const string AdvanceAction = "dialogue_advance";
    public const string SkipAction = "dialogue_skip";
    public const string HistoryAction = "dialogue_history";
    public const string DebugAction = "dialogue_debug";

    private const string BlurShaderPath = "res://assets/shaders/dialogue_blur.gdshader";
    private const float FadeTime = 0.2f;
    private const int MaxChoiceKeys = 9;

    private static readonly Color PanelColor = new(0.07f, 0.08f, 0.11f, 0.9f);
    private static readonly Color NamePlateColor = new(0.55f, 0.36f, 0.16f, 0.95f);
    private static readonly Color BorderColor = new(0.85f, 0.72f, 0.45f);
    private static readonly Color TextColor = new(0.96f, 0.94f, 0.9f);
    private static readonly Color NarrationColor = new(0.8f, 0.82f, 0.9f);

    private readonly Dictionary<StageSlot, PortraitView> _portraits = [];

    private DialogueSession? _session;
    private bool _wasPaused;
    private double _skipCooldown;
    private Tween? _fade;

    private Control? _root;
    private ColorRect? _worldBlur;
    private TextureRect? _backgroundImage;
    private Control? _stage;
    private PanelContainer? _namePlate;
    private Label? _name;
    private Label? _text;
    private Label? _continue;
    private VBoxContainer? _choices;
    private PanelContainer? _history;
    private VBoxContainer? _historyLines;
    private ScrollContainer? _historyScroll;

    /// <summary>The dialogue scene in the tree, once it is ready.</summary>
    public static DialogueScene? Instance { get; private set; }

    /// <summary>Raised when a scene closes, with the script's id.</summary>
    public event Action<string>? Finished;

    /// <summary>How fast lines type out. 0 shows each line at once.</summary>
    [Export(PropertyHint.Range, "0,200,1,suffix:chars/s")]
    public double CharactersPerSecond { get; set; } = 40;

    /// <summary>While Ctrl is held, the time between skipped lines, so skipping stays readable.</summary>
    [Export(PropertyHint.Range, "0,0.5,0.01,suffix:s")]
    public double SkipInterval { get; set; } = 0.06;

    /// <summary>Size of a bust portrait on screen. Portrait art is drawn at this size, see <see cref="PortraitArt"/>.</summary>
    [Export]
    public Vector2 PortraitSize { get; set; } = new(PortraitArt.Width, PortraitArt.Height);

    /// <summary>Script that <b>F8</b> plays, to try the scene out. Empty turns the key off.</summary>
    [Export]
    public string DebugScriptId { get; set; } = "example";

    /// <summary>Whether a scene is playing.</summary>
    public bool IsPlaying => _session is not null;

    /// <summary>The history of the playing scene, or null when none is playing.</summary>
    public DialogueHistory? History => _session?.History;

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (IsPlaying)
        {
            GetTree().Paused = _wasPaused;
        }
    }

    public override void _Ready()
    {
        // Keep running while the world is paused.
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
        Build();
    }

    /// <summary>Plays <c>data/dialogue/&lt;scriptId&gt;.json</c>.</summary>
    /// <returns>Whether it started. A missing or broken file logs an error and returns false.</returns>
    public bool Play(string scriptId)
    {
        var path = $"{DialogueRoot}/{scriptId}.json";
        if (!Godot.FileAccess.FileExists(path))
        {
            GD.PushError($"Dialogue {path} not found.");
            return false;
        }

        try
        {
            Play(DialogueScript.FromJson(Godot.FileAccess.GetFileAsString(path)));
            return true;
        }
        catch (InvalidDataException e)
        {
            GD.PushError($"{path}: {e.Message}");
            return false;
        }
    }

    /// <summary>Plays <paramref name="script"/>, pausing the world until it ends.</summary>
    /// <exception cref="InvalidOperationException">A scene is already playing.</exception>
    public void Play(DialogueScript script)
    {
        ArgumentNullException.ThrowIfNull(script);
        if (IsPlaying)
        {
            throw new InvalidOperationException($"Dialogue '{_session!.Runner.Script.Id}' is still playing.");
        }

        var flags = SaveService.Instance?.Flags;
        if (flags is null)
        {
            GD.PushWarning("No SaveService; dialogue flags from this scene won't be saved.");
            flags = new DialogueFlags();
        }

        ClearStage();
        SetBackground(null);
        ClearChoices();
        ClearHistory();
        ShowHistory(false);
        ShowLine(null, "", 0);

        var session = new DialogueSession(new DialogueRunner(script, flags), CharactersPerSecond);
        session.StageChanged += OnStageChanged;
        session.LineStarted += OnLineStarted;
        session.ChoiceShown += OnChoiceShown;
        session.Ended += OnEnded;
        session.History.Added += AddHistoryLine;
        _session = session;

        _wasPaused = GetTree().Paused;
        GetTree().Paused = true;
        Visible = true;
        Fade(1f, null);
        session.Start();
    }

    public override void _Process(double delta)
    {
        if (_session is not { } session || _history?.Visible == true)
        {
            return;
        }

        var skip = false;
        if (Input.IsActionPressed(SkipAction))
        {
            _skipCooldown -= delta;
            if (_skipCooldown <= 0)
            {
                skip = true;
                _skipCooldown = SkipInterval;
            }
        }
        else
        {
            _skipCooldown = 0;
        }

        session.Update(delta, skip);
        if (_session is null)
        {
            return;
        }

        if (_text is not null)
        {
            _text.VisibleCharacters = session.Typewriter.VisibleCharacters;
        }

        if (_continue is not null)
        {
            var shown = session.State == DialogueSessionState.LineComplete;
            _continue.Visible = shown;
            if (shown)
            {
                // Blink gently.
                _continue.Modulate = Colors.White with { A = 0.55f + (0.45f * Mathf.Sin(Time.GetTicksMsec() / 180f)) };
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_session is not { } session)
        {
            if (!string.IsNullOrEmpty(DebugScriptId) && @event.IsActionPressed(DebugAction))
            {
                Play(DebugScriptId);
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        // A scene swallows all input so nothing reaches the paused world.
        GetViewport().SetInputAsHandled();

        if (@event.IsActionPressed(HistoryAction) || (_history?.Visible == true && @event.IsActionPressed("ui_cancel")))
        {
            ShowHistory(_history?.Visible != true);
            return;
        }

        if (_history?.Visible == true)
        {
            return;
        }

        if (@event.IsActionPressed(AdvanceAction))
        {
            session.Confirm();
        }
        else if (session.State == DialogueSessionState.Choosing
            && @event is InputEventKey { Pressed: true, Echo: false } key
            && key.Keycode >= Key.Key1
            && key.Keycode < Key.Key1 + Math.Min(session.Options.Count, MaxChoiceKeys))
        {
            Choose((int)(key.Keycode - Key.Key1));
        }
    }

    private void OnRootGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } && _session is { } session)
        {
            session.Confirm();
            _root?.AcceptEvent();
        }
    }

    private void OnStageChanged(DialogueEvent change)
    {
        switch (change)
        {
            case CharacterEntered entered:
                RemovePortrait(entered.Slot);
                var portrait = new PortraitView();
                portrait.Setup(entered.Character, entered.Slot, entered.Expression, entered.Flip, PortraitSize);
                _stage?.AddChild(portrait);
                _portraits[entered.Slot] = portrait;
                portrait.SetSpeaking(portrait.Character == _session?.Speaker, instant: true);
                portrait.AnimateIn();
                break;
            case CharacterExited exited:
                RemovePortrait(exited.Slot);
                break;
            case ExpressionChanged expression:
                if (_portraits.TryGetValue(expression.Slot, out var view))
                {
                    view.SetExpression(expression.Expression);
                }

                break;
            case BackgroundChanged background:
                SetBackground(background.Image);
                break;
        }
    }

    private void OnLineStarted()
    {
        if (_session is not { } session)
        {
            return;
        }

        ShowLine(session.Speaker, session.Text, session.Typewriter.VisibleCharacters);
        foreach (var portrait in _portraits.Values)
        {
            portrait.SetSpeaking(portrait.Character == session.Speaker);
        }
    }

    private void OnChoiceShown()
    {
        if (_session is not { } session || _choices is null)
        {
            return;
        }

        ClearChoices();
        if (_continue is not null)
        {
            _continue.Visible = false;
        }

        for (var i = 0; i < session.Options.Count; i++)
        {
            var index = i;
            var prefix = i < MaxChoiceKeys ? $"{i + 1}. " : "";
            var button = new Button { Text = prefix + session.Options[i], CustomMinimumSize = new Vector2(460, 44) };
            button.AddThemeFontSizeOverride("font_size", 20);
            button.Pressed += () => Choose(index);
            _choices.AddChild(button);
        }

        _choices.Visible = true;
        if (_choices.GetChildCount() > 0)
        {
            _choices.GetChild<Button>(0).GrabFocus();
        }
    }

    private void Choose(int index)
    {
        if (_session is not { State: DialogueSessionState.Choosing } session)
        {
            return;
        }

        ClearChoices();
        session.Choose(index);
    }

    private void OnEnded()
    {
        if (_session is not { } session)
        {
            return;
        }

        var id = session.Runner.Script.Id;
        _session = null;
        ClearChoices();
        ShowHistory(false);
        Fade(0f, () =>
        {
            Visible = false;
            ClearStage();
            GetTree().Paused = _wasPaused;
            Finished?.Invoke(id);
        });
    }

    private void ShowLine(string? speaker, string text, int visible)
    {
        if (_text is null || _name is null || _namePlate is null)
        {
            return;
        }

        _namePlate.Visible = speaker is not null;
        _name.Text = speaker is null ? "" : SpeakerNames.Display(speaker);
        _text.Text = text;
        _text.VisibleCharacters = visible;
        _text.AddThemeColorOverride("font_color", speaker is null ? NarrationColor : TextColor);
    }

    private void SetBackground(string? image)
    {
        if (_worldBlur is null || _backgroundImage is null)
        {
            return;
        }

        var texture = image is null ? null : LoadBackground(image);
        _backgroundImage.Texture = texture;
        _backgroundImage.Visible = texture is not null;
        _worldBlur.Visible = texture is null;
    }

    private static Texture2D? LoadBackground(string image)
    {
        var path = image.StartsWith("res://", StringComparison.Ordinal) ? image : $"{BackgroundRoot}/{image}.png";
        if (ResourceLoader.Exists(path))
        {
            return GD.Load<Texture2D>(path);
        }

        GD.PushWarning($"Dialogue background {path} not found; showing the world instead.");
        return null;
    }

    private void RemovePortrait(StageSlot slot)
    {
        if (_portraits.Remove(slot, out var old))
        {
            old.AnimateOutAndFree();
        }
    }

    private void ClearStage()
    {
        foreach (var portrait in _portraits.Values)
        {
            portrait.QueueFree();
        }

        _portraits.Clear();
    }

    private void ClearChoices()
    {
        if (_choices is null)
        {
            return;
        }

        foreach (var child in _choices.GetChildren())
        {
            child.QueueFree();
        }

        _choices.Visible = false;
    }

    private void ClearHistory()
    {
        if (_historyLines is null)
        {
            return;
        }

        foreach (var child in _historyLines.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void AddHistoryLine(HistoryEntry entry)
    {
        if (_historyLines is null)
        {
            return;
        }

        var text = entry switch
        {
            { IsChoice: true } => $"> {entry.Text}",
            { Speaker: { } speaker } => $"{SpeakerNames.Display(speaker)}: {entry.Text}",
            _ => entry.Text,
        };
        var line = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        line.AddThemeFontSizeOverride("font_size", 18);
        line.AddThemeColorOverride(
            "font_color",
            entry.IsChoice ? BorderColor : entry.Speaker is null ? NarrationColor : TextColor);
        _historyLines.AddChild(line);
    }

    private void ShowHistory(bool show)
    {
        if (_history is null)
        {
            return;
        }

        _history.Visible = show;
        if (show && _historyScroll is not null)
        {
            // Scroll to the newest line once the layout has caught up.
            Callable.From(() => _historyScroll.ScrollVertical = (int)_historyScroll.GetVScrollBar().MaxValue).CallDeferred();
        }
    }

    private void Fade(float alpha, Action? then)
    {
        if (_root is null)
        {
            return;
        }

        _fade?.Kill();
        if (alpha > 0)
        {
            _root.Modulate = Colors.White with { A = 0 };
        }

        _fade = CreateTween();
        _fade.TweenProperty(_root, "modulate:a", alpha, FadeTime);
        if (then is not null)
        {
            _fade.TweenCallback(Callable.From(then));
        }
    }

    private void Build()
    {
        Layer = 10;

        _root = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.GuiInput += OnRootGuiInput;

        _worldBlur = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
        if (ResourceLoader.Exists(BlurShaderPath))
        {
            _worldBlur.Material = new ShaderMaterial { Shader = GD.Load<Shader>(BlurShaderPath) };
        }
        else
        {
            _worldBlur.Color = new Color(0, 0, 0, 0.55f);
        }

        _root.AddChild(_worldBlur);
        _worldBlur.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _backgroundImage = new TextureRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            Visible = false,
        };
        _root.AddChild(_backgroundImage);
        _backgroundImage.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _stage = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_stage);
        _stage.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        BuildTextBox(_root);
        BuildChoices(_root);
        BuildTopBar(_root);
        BuildHistory(_root);
    }

    private void BuildTextBox(Control root)
    {
        const int margin = 24;
        const int height = 170;

        var box = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddThemeStyleboxOverride("panel", Panel(PanelColor, border: 2, padding: 18));
        root.AddChild(box);
        box.AnchorLeft = 0;
        box.AnchorRight = 1;
        box.AnchorTop = 1;
        box.AnchorBottom = 1;
        box.OffsetLeft = margin;
        box.OffsetRight = -margin;
        box.OffsetTop = -height - margin;
        box.OffsetBottom = -margin;

        _text = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Top,
            VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
        };
        _text.AddThemeFontSizeOverride("font_size", 22);
        _text.AddThemeColorOverride("font_color", TextColor);
        box.AddChild(_text);

        _continue = new Label { Text = "▼", MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        _continue.AddThemeFontSizeOverride("font_size", 18);
        _continue.AddThemeColorOverride("font_color", BorderColor);
        root.AddChild(_continue);
        _continue.AnchorLeft = 1;
        _continue.AnchorRight = 1;
        _continue.AnchorTop = 1;
        _continue.AnchorBottom = 1;
        _continue.OffsetLeft = -margin - 40;
        _continue.OffsetRight = -margin - 16;
        _continue.OffsetTop = -margin - 36;
        _continue.OffsetBottom = -margin - 8;

        _namePlate = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        _namePlate.AddThemeStyleboxOverride("panel", Panel(NamePlateColor, border: 2, padding: 8));
        root.AddChild(_namePlate);
        _namePlate.AnchorLeft = 0;
        _namePlate.AnchorRight = 0;
        _namePlate.AnchorTop = 1;
        _namePlate.AnchorBottom = 1;
        _namePlate.OffsetLeft = margin + 16;
        _namePlate.OffsetTop = -height - margin - 26;
        _namePlate.OffsetBottom = -height - margin + 14;
        _namePlate.GrowHorizontal = Control.GrowDirection.End;

        _name = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        _name.AddThemeFontSizeOverride("font_size", 20);
        _name.AddThemeColorOverride("font_color", Colors.White);
        _namePlate.AddChild(_name);
    }

    private void BuildChoices(Control root)
    {
        _choices = new VBoxContainer { Visible = false, Alignment = BoxContainer.AlignmentMode.Center };
        _choices.AddThemeConstantOverride("separation", 10);
        root.AddChild(_choices);
        _choices.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _choices.GrowHorizontal = Control.GrowDirection.Both;
        _choices.GrowVertical = Control.GrowDirection.Both;
        _choices.OffsetTop -= 150;
        _choices.OffsetBottom -= 150;
    }

    private void BuildTopBar(Control root)
    {
        var bar = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AddThemeConstantOverride("separation", 12);
        root.AddChild(bar);
        bar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        bar.GrowHorizontal = Control.GrowDirection.Begin;
        bar.OffsetTop = 16;
        bar.OffsetRight = -16;

        var hint = new Label { Text = "Hold Ctrl to skip", MouseFilter = Control.MouseFilterEnum.Ignore };
        hint.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.6f));
        bar.AddChild(hint);

        var log = new Button { Text = "Log (H)", FocusMode = Control.FocusModeEnum.None };
        log.Pressed += () => ShowHistory(_history?.Visible != true);
        bar.AddChild(log);
    }

    private void BuildHistory(Control root)
    {
        _history = new PanelContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        _history.AddThemeStyleboxOverride("panel", Panel(PanelColor with { A = 0.96f }, border: 2, padding: 20));
        root.AddChild(_history);
        _history.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _history.OffsetLeft = 80;
        _history.OffsetRight = -80;
        _history.OffsetTop = 60;
        _history.OffsetBottom = -60;

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        _history.AddChild(column);

        var title = new Label { Text = "History  (H or Esc to close)" };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", BorderColor);
        column.AddChild(title);

        _historyScroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        column.AddChild(_historyScroll);

        _historyLines = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _historyLines.AddThemeConstantOverride("separation", 6);
        _historyScroll.AddChild(_historyLines);
    }

    private static StyleBoxFlat Panel(Color color, int border, int padding)
    {
        var style = new StyleBoxFlat
        {
            BgColor = color,
            BorderColor = BorderColor,
            ContentMarginLeft = padding,
            ContentMarginRight = padding,
            ContentMarginTop = padding,
            ContentMarginBottom = padding,
        };
        style.SetBorderWidthAll(border);
        style.SetCornerRadiusAll(6);
        return style;
    }
}

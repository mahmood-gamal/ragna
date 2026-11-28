using Godot;
using Ragna.Core.Combat;
using Ragna.Core.Hud;
using Ragna.Core.Items;
using Ragna.Gameplay.Combat;
using Ragna.Gameplay.Items;

namespace Ragna.Ui;

/// <summary>
/// The in-game HUD: the player's level with HP, SP and EXP bars in the top-right corner, and a
/// message log in the bottom-left corner. Nothing is polled: the bars and the log update from the
/// player's events (<see cref="Health.Changed"/>, <see cref="SpPool.Changed"/>,
/// <see cref="Core.Stats.CharacterStats.ExpChanged"/>, <see cref="PlayerCombat"/> and
/// <see cref="PlayerPickup"/> events). What the bars and lines say lives in
/// <see cref="BarReading"/> and <see cref="HudMessages"/>.
/// </summary>
/// <remarks>
/// The HUD ignores the mouse, so clicks go through it to the world. Other nodes can write to the
/// log with <see cref="Post"/>.
/// </remarks>
public partial class Hud : Control
{
    // Gap between the HUD panels and the screen edge.
    private const int ScreenMargin = 16;

    private static readonly Color HpColor = new(0.85f, 0.22f, 0.22f);
    private static readonly Color SpColor = new(0.25f, 0.45f, 0.95f);
    private static readonly Color ExpColor = new(0.95f, 0.78f, 0.2f);
    private static readonly Color PanelColor = new(0.08f, 0.09f, 0.11f, 0.78f);

    private static readonly Dictionary<MessageKind, Color> KindColors = new()
    {
        [MessageKind.System] = new Color(0.85f, 0.85f, 0.85f),
        [MessageKind.DamageDealt] = new Color(1f, 1f, 1f),
        [MessageKind.DamageTaken] = new Color(1f, 0.55f, 0.5f),
        [MessageKind.Loot] = new Color(0.6f, 0.9f, 0.55f),
        [MessageKind.LevelUp] = new Color(1f, 0.85f, 0.3f),
    };

    private PlayerCombat? _player;
    private PlayerPickup? _pickup;
    private Label? _level;
    private HudBar? _hp;
    private HudBar? _sp;
    private HudBar? _exp;
    private VBoxContainer? _lines;

    /// <summary>The player's combat node, which holds its HP, SP and stats.</summary>
    [Export]
    public PlayerCombat? Player { get; set; }

    /// <summary>The player's pick-up node, for loot messages. Optional.</summary>
    [Export]
    public PlayerPickup? Pickup { get; set; }

    /// <summary>Most lines the log shows at once. Older ones scroll off.</summary>
    [Export(PropertyHint.Range, "1,30,1")]
    public int VisibleLines { get; set; } = 8;

    /// <summary>Width of the HP, SP and EXP bars.</summary>
    [Export(PropertyHint.Range, "80,600,1,suffix:px")]
    public int BarWidth { get; set; } = 220;

    /// <summary>Width of the message log.</summary>
    [Export(PropertyHint.Range, "160,1200,1,suffix:px")]
    public int LogWidth { get; set; } = 420;

    /// <summary>The messages, newest last. Keeps more than <see cref="VisibleLines"/>.</summary>
    public MessageLog Log { get; } = new();

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        BuildStatus();
        BuildLog();
        Log.Added += OnMessageAdded;

        if (Player is null)
        {
            GD.PushWarning($"{Name}: no Player set; the bars stay empty.");
            return;
        }

        _player = Player;
        _player.Health.Changed += RefreshHp;
        _player.Sp.Changed += RefreshSp;
        _player.Stats.ExpChanged += RefreshExp;
        _player.Stats.LevelledUp += OnLevelledUp;
        _player.AttackMade += OnAttackMade;
        _player.Attacked += OnAttacked;
        _player.Killed += OnKilled;
        _player.Died += OnDied;
        _player.Revived += OnRevived;

        _pickup = Pickup;
        if (_pickup is not null)
        {
            _pickup.PickedUp += OnPickedUp;
        }

        RefreshHp();
        RefreshSp();
        RefreshExp();
    }

    public override void _ExitTree()
    {
        Log.Added -= OnMessageAdded;
        if (_player is not null)
        {
            _player.Health.Changed -= RefreshHp;
            _player.Sp.Changed -= RefreshSp;
            _player.Stats.ExpChanged -= RefreshExp;
            _player.Stats.LevelledUp -= OnLevelledUp;
            _player.AttackMade -= OnAttackMade;
            _player.Attacked -= OnAttacked;
            _player.Killed -= OnKilled;
            _player.Died -= OnDied;
            _player.Revived -= OnRevived;
        }

        if (_pickup is not null)
        {
            _pickup.PickedUp -= OnPickedUp;
        }
    }

    /// <summary>Adds a line to the message log.</summary>
    public void Post(MessageKind kind, string text) => Log.Add(kind, text);

    private void OnAttackMade(AttackResult result, Monster target) => Log.Add(HudMessages.DamageDealt(result, target.DisplayName));

    private void OnAttacked(AttackResult result, Monster attacker) => Log.Add(HudMessages.DamageTaken(result, attacker.DisplayName));

    private void OnKilled(Monster monster, int exp) => Log.Add(HudMessages.Defeated(monster.DisplayName, exp));

    private void OnDied(Monster killer) => Log.Add(HudMessages.Died(killer.DisplayName));

    private void OnRevived() => Log.Add(HudMessages.Revived());

    private void OnPickedUp(ItemDefinition item, int quantity) => Log.Add(HudMessages.PickedUp(item.Name, quantity));

    private void OnLevelledUp(int level)
    {
        Log.Add(HudMessages.LevelUp(level, _player?.Stats.StatPoints ?? 0));
        RefreshLevel();
    }

    private void RefreshHp()
    {
        if (_player is not null)
        {
            _hp?.Display(BarReading.Hp(_player.Health));
        }
    }

    private void RefreshSp()
    {
        if (_player is not null)
        {
            _sp?.Display(BarReading.Sp(_player.Sp));
        }
    }

    private void RefreshExp()
    {
        if (_player is not null)
        {
            _exp?.Display(BarReading.Exp(_player.Stats));
            RefreshLevel();
        }
    }

    private void RefreshLevel()
    {
        if (_player is not null && _level is not null)
        {
            _level.Text = $"Base Lv. {_player.Stats.Level}";
        }
    }

    private void OnMessageAdded(LogMessage _)
    {
        if (_lines is null)
        {
            return;
        }

        var latest = Log.Latest(VisibleLines);
        while (_lines.GetChildCount() < latest.Count)
        {
            var label = new Label { MouseFilter = MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            label.AddThemeFontSizeOverride("font_size", 14);
            label.AddThemeConstantOverride("outline_size", 4);
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            _lines.AddChild(label);
        }

        for (var i = 0; i < latest.Count; i++)
        {
            var label = (Label)_lines.GetChild(i);
            label.Text = latest[i].Text;
            label.AddThemeColorOverride("font_color", KindColors[latest[i].Kind]);
        }
    }

    private void BuildStatus()
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", PanelStyle());
        panel.GrowHorizontal = GrowDirection.Begin;
        AddChild(panel);

        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);

        _level = new Label { MouseFilter = MouseFilterEnum.Ignore, Text = "Base Lv. 1" };
        column.AddChild(_level);

        _hp = AddBar(column, HpColor);
        _sp = AddBar(column, SpColor);
        _exp = AddBar(column, ExpColor);
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight, LayoutPresetMode.Minsize, ScreenMargin);
    }

    private HudBar AddBar(Container parent, Color fill)
    {
        var bar = new HudBar();
        bar.Setup(fill, BarWidth);
        parent.AddChild(bar);
        return bar;
    }

    private void BuildLog()
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(LogWidth, 0) };
        panel.AddThemeStyleboxOverride("panel", PanelStyle());
        panel.GrowVertical = GrowDirection.Begin;
        AddChild(panel);

        _lines = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _lines.AddThemeConstantOverride("separation", 0);
        panel.AddChild(_lines);
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft, LayoutPresetMode.Minsize, ScreenMargin);
    }

    private static StyleBoxFlat PanelStyle()
    {
        var style = new StyleBoxFlat { BgColor = PanelColor };
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(8);
        return style;
    }
}

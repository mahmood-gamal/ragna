using Godot;
using Ragna.Core.Combat;
using Ragna.Core.Hud;
using Ragna.Gameplay.Combat;

namespace Ragna.Ui;

/// <summary>
/// Combat feedback in the world: floating damage numbers over whoever is hit, and an HP bar over the
/// player's current target. Driven only by events: <see cref="PlayerCombat.AttackMade"/> and
/// <see cref="PlayerCombat.Attacked"/> spawn numbers, <see cref="PlayerCombat.TargetChanged"/> moves
/// the HP bar, and the bar follows the target's <see cref="Health.Changed"/>. Nothing is polled.
/// See docs/combat.md.
/// </summary>
public partial class CombatFeedback : Node3D
{
    private PlayerCombat? _player;
    private TargetHpBar? _bar;
    private int _spawned;

    /// <summary>The player's combat node.</summary>
    [Export]
    public PlayerCombat? Player { get; set; }

    /// <summary>Height above a character's feet where its damage numbers appear.</summary>
    [Export(PropertyHint.Range, "0,5,0.05,suffix:m")]
    public float NumberHeight { get; set; } = 2.05f;

    /// <summary>Height above the target's feet where its HP bar floats.</summary>
    [Export(PropertyHint.Range, "0,5,0.05,suffix:m")]
    public float BarHeight { get; set; } = 1.8f;

    /// <summary>Whether to show damage numbers over the player when monsters attack.</summary>
    [Export]
    public bool ShowDamageTaken { get; set; } = true;

    /// <summary>The HP bar over the current target, if there is one.</summary>
    public TargetHpBar? Bar => _bar is not null && IsInstanceValid(_bar) ? _bar : null;

    public override void _Ready()
    {
        if (Player is null)
        {
            GD.PushWarning($"{Name}: no Player set; no damage numbers or target HP bar.");
            return;
        }

        _player = Player;
        _player.AttackMade += OnAttackMade;
        _player.Attacked += OnAttacked;
        _player.TargetChanged += OnTargetChanged;
    }

    public override void _ExitTree()
    {
        if (_player is not null)
        {
            _player.AttackMade -= OnAttackMade;
            _player.Attacked -= OnAttacked;
            _player.TargetChanged -= OnTargetChanged;
        }
    }

    /// <summary>Shows <paramref name="popup"/> over <paramref name="over"/>.</summary>
    public void Spawn(DamagePopup popup, Node3D over)
    {
        ArgumentNullException.ThrowIfNull(over);

        // Alternate sides, so quick hits on one target don't stack on top of each other.
        var side = (_spawned++ % 2 == 0) ? 1 : -1;
        var number = new DamageNumber();
        number.Setup(popup, over.GlobalPosition + (Vector3.Up * NumberHeight), side);
        AddChild(number);
    }

    private void OnAttackMade(AttackResult result, Monster target) => Spawn(DamagePopup.OnTarget(result), target);

    private void OnAttacked(AttackResult result, Monster attacker)
    {
        if (ShowDamageTaken && _player is not null)
        {
            Spawn(DamagePopup.OnPlayer(result), _player.Body);
        }
    }

    private void OnTargetChanged(Monster? target)
    {
        if (Bar is { } old)
        {
            old.QueueFree();
        }

        _bar = null;
        if (target is null || !IsInstanceValid(target) || target.IsDead)
        {
            return;
        }

        _bar = new TargetHpBar { Name = "TargetHpBar", Position = Vector3.Up * BarHeight };
        _bar.Bind(target.Health);
        target.AddChild(_bar);
    }
}

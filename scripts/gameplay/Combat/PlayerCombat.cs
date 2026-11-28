using Godot;
using Ragna.Core.Combat;
using Ragna.Core.Save;
using Ragna.Core.Stats;
using Ragna.Gameplay.Characters;
using Ragna.Gameplay.Movement;
using NVector3 = System.Numerics.Vector3;

namespace Ragna.Gameplay.Combat;

/// <summary>
/// Ragnarok-style click-to-attack for the player. Left-click a <see cref="Monster"/>: the player
/// walks into range along the navmesh, then attacks it every attack delay until it dies or the
/// ground is clicked. Attack speed comes from the player's stats; damage from
/// <see cref="DamageCalculator"/>. The rules live in <see cref="AutoAttack"/>.
/// </summary>
/// <remarks>
/// Put it as a child of the player's <see cref="ClickToMoveController"/>. Godot sends unhandled input
/// to children before their parent, so a click on a monster is taken here and never reaches the
/// mover; a click on the ground drops the target and goes on to the mover.
/// </remarks>
public partial class PlayerCombat : Node
{
    /// <summary>Group the player's combat node is in, so monsters can find it.</summary>
    public const string Group = "player";

    private const float PickRayLength = 1000f;
    private const string StatTablePath = "res://data/stats/stat_table.json";

    private AutoAttack _autoAttack = new();
    private DamageCalculator _damage = new();
    private ClickToMoveController _body = null!;
    private CharacterSprite? _sprite;
    private double _reviveIn;

    /// <summary>Raised after each attack the player makes, with the target.</summary>
    public event Action<AttackResult, Monster>? AttackMade;

    /// <summary>Raised when the player's target dies, with the EXP gained.</summary>
    public event Action<Monster, int>? Killed;

    /// <summary>Raised when the target changes. <c>null</c> when the player stops attacking.</summary>
    public event Action<Monster?>? TargetChanged;

    /// <summary>Raised when a monster attacks the player, hit or miss, with the attacker.</summary>
    public event Action<AttackResult, Monster>? Attacked;

    /// <summary>Raised once when the player's HP reaches 0, with the killer.</summary>
    public event Action<Monster>? Died;

    /// <summary>Raised when the player comes back to life after dying.</summary>
    public event Action? Revived;

    /// <summary>The sprite to face the target and play <c>attack</c>. Defaults to the first one under the player.</summary>
    [Export]
    public CharacterSprite? Sprite { get; set; }

    /// <summary>Ground distance between centres at which the player stops and swings.</summary>
    [Export(PropertyHint.Range, "0.5,20,0.1,suffix:m")]
    public float AttackRange { get; set; } = 1.5f;

    /// <summary>Physics layers the click ray can hit. Terrain (1) blocks it; monsters (3) are targets.</summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint PickMask { get; set; } = 1u | Monster.MonsterLayer;

    /// <summary>
    /// Seconds after dying before the player is back on their feet with full HP, where they fell.
    /// A stand-in until there is a proper death screen.
    /// </summary>
    [Export(PropertyHint.Range, "0,60,0.5,suffix:s")]
    public float ReviveTime { get; set; } = 5f;

    [ExportGroup("Starting stats")]
    [Export(PropertyHint.Range, "1,99,1")]
    public int Str { get; set; } = 10;

    [Export(PropertyHint.Range, "1,99,1")]
    public int Agi { get; set; } = 10;

    [Export(PropertyHint.Range, "1,99,1")]
    public int Vit { get; set; } = 5;

    [Export(PropertyHint.Range, "1,99,1")]
    public int Int { get; set; } = 1;

    [Export(PropertyHint.Range, "1,99,1")]
    public int Dex { get; set; } = 10;

    [Export(PropertyHint.Range, "1,99,1")]
    public int Luk { get; set; } = 5;

    /// <summary>ATK of the equipped weapon. Equipment comes later.</summary>
    [Export(PropertyHint.Range, "0,1000,1")]
    public int WeaponAtk { get; set; } = 15;

    /// <summary>The player's level, EXP and stats.</summary>
    public CharacterStats Stats { get; private set; } = new(new StatTable());

    /// <summary>The monster being attacked, if any.</summary>
    public Monster? Target { get; private set; }

    public CombatStats CombatStats => CombatStats.FromCharacter(Stats, WeaponAtk);

    /// <summary>The player's HP. Max HP follows <see cref="Stats"/>.</summary>
    public Health Health { get; private set; } = new(1);

    /// <summary>The player's SP. Max SP follows <see cref="Stats"/>. Skills will spend it.</summary>
    public SpPool Sp { get; private set; } = new(0);

    public bool IsDead => Health.IsDead;

    /// <summary>The player's body, for its position.</summary>
    public Node3D Body => _body;

    public override void _Ready()
    {
        _body = GetParent() as ClickToMoveController
            ?? throw new InvalidOperationException($"{Name} must be a child of a ClickToMoveController.");
        _sprite = Sprite ?? FindSprite(_body);
        _autoAttack = new AutoAttack(new AutoAttackSettings { Range = AttackRange });

        var table = LoadStatTable();
        Stats = new CharacterStats(table, 1, 0, new BaseStats(Str, Agi, Vit, Int, Dex, Luk), statPoints: 0);
        Health = new Health(Stats.Derived.MaxHp);
        Sp = new SpPool(Stats.Derived.MaxSp);
        Stats.Changed += () =>
        {
            Health.SetMax(Stats.Derived.MaxHp);
            Sp.SetMax(Stats.Derived.MaxSp);
        };
        AddToGroup(Group);
    }

    /// <summary>Applies one attack from <paramref name="attacker"/>. Does nothing once dead.</summary>
    public void ReceiveAttack(AttackResult result, Monster attacker)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        if (IsDead)
        {
            return;
        }

        Health.TakeDamage(result.Damage);
        Attacked?.Invoke(result, attacker);
        if (!IsDead)
        {
            return;
        }

        StopAttacking();
        _body.StopMoving();
        _reviveIn = ReviveTime;
        Died?.Invoke(attacker);
        GD.Print($"Killed by {attacker.DisplayName}. Back on your feet in {ReviveTime:0.#} s.");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed(ClickToMoveController.MoveAction))
        {
            return;
        }

        if (IsDead)
        {
            // The dead neither walk nor fight.
            GetViewport().SetInputAsHandled();
            return;
        }

        var screenPoint = @event is InputEventMouse mouse ? mouse.Position : GetViewport().GetMousePosition();
        if (PickMonster(screenPoint) is { } monster)
        {
            Attack(monster);
            GetViewport().SetInputAsHandled();
        }
        else
        {
            // A click on the ground: stop attacking and let the mover walk there.
            StopAttacking();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
        {
            _reviveIn -= delta;
            if (_reviveIn <= 0)
            {
                Health.Revive();
                Sp.Refill();
                Revived?.Invoke();
            }

            return;
        }

        if (Target is { } target && !IsInstanceValid(target))
        {
            SetTarget(null);
        }

        if (Target is null && !_autoAttack.IsEngaged)
        {
            // Keep the cooldown running between fights.
            _autoAttack.Tick((float)delta, NVector3.Zero, NVector3.Zero, false, Stats.Derived.AttackDelay);
            return;
        }

        var position = _body.GlobalPosition;
        var targetPosition = Target?.GlobalPosition ?? position;
        var step = _autoAttack.Tick(
            (float)delta,
            ToCore(position),
            ToCore(targetPosition),
            Target is { IsDead: false },
            Stats.Derived.AttackDelay);

        switch (step.Action)
        {
            case AutoAttackAction.None:
                SetTarget(null);
                break;
            case AutoAttackAction.Chase when step.Repath:
                _body.MoveTo(targetPosition);
                break;
            case AutoAttackAction.Wait:
                StandAndFace(targetPosition - position);
                break;
            case AutoAttackAction.Attack:
                StandAndFace(targetPosition - position);
                Swing(Target!);
                break;
        }
    }

    /// <summary>The player's level, stats, HP, SP and position, for a save file.</summary>
    public PlayerSave CaptureSave() => SaveMapper.CapturePlayer(Stats, Health, Sp, ToCore(_body.GlobalPosition));

    /// <summary>
    /// Restores the player from <paramref name="save"/>: stops attacking and walking, sets stats,
    /// HP and SP in place (so the HUD keeps following them) and moves the player to the saved
    /// position. A player who was dead is brought back.
    /// </summary>
    /// <exception cref="InvalidDataException">The saved stats are out of range; nothing changes.</exception>
    public void RestoreSave(PlayerSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        var wasDead = IsDead;
        SaveMapper.RestorePlayer(save, Stats, Health, Sp);
        StopAttacking();
        _body.Teleport(new Vector3(save.Position.X, save.Position.Y, save.Position.Z));
        _reviveIn = 0;
        if (wasDead)
        {
            Revived?.Invoke();
        }
    }

    /// <summary>Starts attacking <paramref name="monster"/>: walk into range, then attack until it dies.</summary>
    public void Attack(Monster monster)
    {
        ArgumentNullException.ThrowIfNull(monster);
        if (monster.IsDead || IsDead)
        {
            return;
        }

        SetTarget(monster);
        _autoAttack.Engage();
    }

    /// <summary>Drops the target. The player stops attacking but keeps any walk already ordered.</summary>
    public void StopAttacking()
    {
        _autoAttack.Disengage();
        SetTarget(null);
    }

    private void StandAndFace(Vector3 direction)
    {
        // Only once: a new navigation target every frame would ask for a new path every frame.
        if (_body.Mover.Destination is not null)
        {
            _body.StopMoving();
        }

        _sprite?.Face(direction);
    }

    private void Swing(Monster target)
    {
        _sprite?.PlayAttack();
        var result = _damage.Attack(CombatStats, target.CombatStats);
        target.ReceiveAttack(result, _body);
        AttackMade?.Invoke(result, target);

        if (target.IsDead)
        {
            // Killed first, so the log reads "defeated, +EXP" before any "level up".
            var exp = target.ExpReward;
            Killed?.Invoke(target, exp);
            Stats.AddExp(exp);
            GD.Print($"{target.DisplayName} defeated. +{exp} EXP (level {Stats.Level}, {Stats.Exp}/{Stats.ExpToNextLevel}).");
        }
    }

    private void SetTarget(Monster? monster)
    {
        if (Target == monster)
        {
            return;
        }

        Target = monster;
        TargetChanged?.Invoke(monster);
    }

    /// <summary>The living monster under <paramref name="screenPoint"/>, unless terrain is in front of it.</summary>
    private Monster? PickMonster(Vector2 screenPoint)
    {
        var camera = _body.Camera ?? GetViewport().GetCamera3D();
        if (camera is null)
        {
            return null;
        }

        var from = camera.ProjectRayOrigin(screenPoint);
        var to = from + (camera.ProjectRayNormal(screenPoint) * PickRayLength);
        var query = PhysicsRayQueryParameters3D.Create(from, to, PickMask, [_body.GetRid()]);
        var hit = _body.GetWorld3D().DirectSpaceState.IntersectRay(query);
        return hit.Count > 0 && hit["collider"].AsGodotObject() is Monster { IsDead: false } monster ? monster : null;
    }

    private static StatTable LoadStatTable()
    {
        if (!Godot.FileAccess.FileExists(StatTablePath))
        {
            GD.PushWarning($"{StatTablePath} not found; using default stats.");
            return new StatTable();
        }

        return StatTableLoader.FromJson(Godot.FileAccess.GetFileAsString(StatTablePath));
    }

    private static CharacterSprite? FindSprite(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is CharacterSprite sprite)
            {
                return sprite;
            }

            if (FindSprite(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static NVector3 ToCore(Vector3 v) => new(v.X, v.Y, v.Z);
}

using Godot;
using Ragna.Core.Combat;
using Ragna.Core.Monsters;
using Ragna.Gameplay.Characters;
using Ragna.Gameplay.Items;
using Ragna.Gameplay.Monsters;
using NVector3 = System.Numerics.Vector3;

namespace Ragna.Gameplay.Combat;

/// <summary>
/// A monster the player can fight. Its type (stats, AI tuning, respawn time) comes from
/// <c>data/monsters/monsters.json</c> by <see cref="MonsterId"/>. It wanders around its home, chases
/// and attacks the player when aggroed or hit, and walks home when the player gets away. The rules
/// live in <see cref="MonsterBrain"/>; this node walks the navmesh, plays the sprite and deals damage.
/// It sits on the <c>monsters</c> physics layer (layer 3) so the player's click ray can find it.
/// Usually spawned by a <see cref="MonsterSpawner"/>, which brings a new one after it dies.
/// </summary>
public partial class Monster : CharacterBody3D
{
    /// <summary>Physics layer bit of the <c>monsters</c> layer (layer 3).</summary>
    public const uint MonsterLayer = 1u << 2;

    private static readonly Color HitFlash = new(1f, 0.45f, 0.45f);

    // How far above and below its feet the body looks for the ground after a step.
    private const float GroundProbeUp = 0.6f;
    private const float GroundProbeDown = 1.5f;

    private CharacterSprite? _sprite;
    private NavigationAgent3D? _agent;
    private MonsterBrain? _brain;
    private DamageCalculator _damage = new();
    private PlayerCombat? _player;
    private bool _grounded;
    private bool _moving;
    private float _speed;
    private Color _tint = Colors.White;

    /// <summary>Raised when an attack lands or misses, with the attacker. For damage numbers (GAM-12) and logs.</summary>
    public event Action<AttackResult, Node3D>? Attacked;

    /// <summary>Raised once when HP reaches 0, with the killer.</summary>
    public event Action<Monster, Node3D>? Died;

    /// <summary>Monster type in <c>data/monsters/monsters.json</c>, for example <c>slime</c>.</summary>
    [Export]
    public string MonsterId { get; set; } = "slime";

    /// <summary>Seconds the body stays on screen after dying.</summary>
    [Export(PropertyHint.Range, "0,10,0.1,suffix:s")]
    public float CorpseTime { get; set; } = 0.6f;

    /// <summary>Scene for the items it drops on death. Defaults to <c>scenes/items/GroundItem.tscn</c>.</summary>
    [Export]
    public PackedScene? GroundItemScene { get; set; }

    /// <summary>Physics layers the ground probe can hit. Layer 1 is the terrain.</summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint GroundMask { get; set; } = 1;

    /// <summary>
    /// Where the monster wanders around and leashes back to. Set it before adding the monster to
    /// the tree; otherwise it is where the monster stands when it is ready.
    /// </summary>
    public Vector3? Home { get; set; }

    public MonsterDefinition Definition { get; private set; } = new() { Id = "monster", Name = "Monster" };

    public string DisplayName => Definition.Name;

    public int Level => Definition.Level;

    /// <summary>EXP the killer gets.</summary>
    public int ExpReward => Definition.ExpReward;

    public CombatStats CombatStats { get; private set; } = new(1, 0, 0, 0, 0, 0);

    public Health Health { get; private set; } = new(1);

    public bool IsDead => Health.IsDead;

    /// <summary>What the AI is doing, for debugging and UI.</summary>
    public MonsterState State => _brain?.State ?? MonsterState.Idle;

    public override void _Ready()
    {
        Definition = MonsterData.Get(MonsterId);
        CombatStats = Definition.CombatStats;
        Health = new Health(Definition.MaxHp);
        _sprite = FindChild<CharacterSprite>(this);
        _agent = FindChild<NavigationAgent3D>(this);
        if (MonsterDefinition.TryParseTint(Definition.Tint, out var r, out var g, out var b))
        {
            _tint = new Color(r, g, b);
        }

        if (_sprite is not null)
        {
            _sprite.Modulate = _tint;
        }

        if (_agent is null)
        {
            GD.PushWarning($"{Name} has no NavigationAgent3D child, so it can't move.");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = (float)delta;
        if (!_grounded)
        {
            // Settle on the ground once, after the physics space has the terrain in it.
            _grounded = true;
            if (GroundHeight(GlobalPosition, 20f, 20f) is { } y)
            {
                GlobalPosition = GlobalPosition with { Y = y };
            }

            Home ??= GlobalPosition;
            _brain = new MonsterBrain(Definition.Behavior, Definition.AttackDelay, ToCore(Home.Value));
            _brain.ReturnedHome += OnReturnedHome;
            if (_agent is not null)
            {
                _agent.TargetPosition = GlobalPosition;
            }

            return;
        }

        if (_brain is null || IsDead)
        {
            Velocity = Vector3.Zero;
            return;
        }

        var player = FindPlayer();
        MonsterTarget? target = player is null
            ? null
            : new MonsterTarget(ToCore(player.Body.GlobalPosition), !player.IsDead);
        var step = _brain.Tick(dt, ToCore(GlobalPosition), target);

        if (step.FaceTarget && player is not null)
        {
            _sprite?.Face(player.Body.GlobalPosition - GlobalPosition);
        }

        switch (step.Action)
        {
            case MonsterAction.Move:
                if (step.Repath || !_moving)
                {
                    StartMoving(ToGodot(step.Destination), step.Run);
                }

                Walk(dt);
                break;
            case MonsterAction.Attack:
                StopMoving();
                if (player is not null)
                {
                    Swing(player);
                }

                break;
            default:
                StopMoving();
                break;
        }
    }

    /// <summary>Applies one attack from <paramref name="attacker"/> and fights back. Does nothing once dead.</summary>
    public void ReceiveAttack(AttackResult result, Node3D attacker)
    {
        if (IsDead)
        {
            return;
        }

        Health.TakeDamage(result.Damage);
        _brain?.Provoke();
        Attacked?.Invoke(result, attacker);
        if (result.Landed)
        {
            Flash();
        }

        _sprite?.Face(attacker.GlobalPosition - GlobalPosition);
        if (IsDead)
        {
            Die(attacker);
        }
    }

    private void Swing(PlayerCombat player)
    {
        _sprite?.PlayAttack();
        var result = _damage.Attack(CombatStats, player.CombatStats);
        player.ReceiveAttack(result, this);
    }

    private void StartMoving(Vector3 destination, bool run)
    {
        _speed = run ? Definition.ChaseSpeed : Definition.MoveSpeed;
        _moving = true;
        if (_agent is not null)
        {
            _agent.TargetPosition = destination;
        }
    }

    private void Walk(float dt)
    {
        if (_agent is null || dt <= 0f)
        {
            return;
        }

        if (_agent.IsNavigationFinished())
        {
            StopMoving();
            _brain?.NotifyArrived();
            return;
        }

        var to = _agent.GetNextPathPosition() - GlobalPosition;
        to.Y = 0f;
        var distance = to.Length();
        if (distance < 1e-4f)
        {
            Velocity = Vector3.Zero;
            return;
        }

        // Don't overshoot the next path point.
        Velocity = to / distance * Mathf.Min(_speed, distance / dt);
        var position = GlobalPosition + (Velocity * dt);
        if (GroundHeight(position, GroundProbeUp, GroundProbeDown) is { } y)
        {
            position.Y = y;
        }

        GlobalPosition = position;
    }

    private void StopMoving()
    {
        Velocity = Vector3.Zero;
        if (!_moving)
        {
            return;
        }

        _moving = false;
        if (_agent is not null)
        {
            _agent.TargetPosition = GlobalPosition;
        }
    }

    private void OnReturnedHome()
    {
        if (Definition.Behavior.HealOnReturn)
        {
            Health.Revive();
        }
    }

    private void Die(Node3D killer)
    {
        // Stop being clickable straight away.
        CollisionLayer = 0;
        StopMoving();
        _brain?.Die();
        Died?.Invoke(this, killer);
        DropLoot();

        var tween = CreateTween();
        if (_sprite is not null)
        {
            tween.TweenProperty(_sprite, "modulate:a", 0f, CorpseTime);
        }
        else
        {
            tween.TweenInterval(CorpseTime);
        }

        tween.TweenCallback(Callable.From(QueueFree));
    }

    private void DropLoot()
    {
        // Next to the monster, not under it, so the items stay after the body is freed.
        if (GetParent() is Node3D parent)
        {
            LootDropper.Drop(Definition, parent, GlobalPosition, GroundItemScene);
        }
    }

    private void Flash()
    {
        if (_sprite is null)
        {
            return;
        }

        _sprite.Modulate = HitFlash * _tint;
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", _tint, 0.2f);
    }

    private PlayerCombat? FindPlayer()
    {
        if (_player is null || !IsInstanceValid(_player))
        {
            _player = GetTree().GetFirstNodeInGroup(PlayerCombat.Group) as PlayerCombat;
        }

        return _player;
    }

    private float? GroundHeight(Vector3 feet, float up, float down)
    {
        var query = PhysicsRayQueryParameters3D.Create(feet + (Vector3.Up * up), feet + (Vector3.Down * down), GroundMask, [GetRid()]);
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        return hit.Count > 0 ? hit["position"].AsVector3().Y : null;
    }

    private static T? FindChild<T>(Node node)
        where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T found)
            {
                return found;
            }

            if (FindChild<T>(child) is { } deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    private static NVector3 ToCore(Vector3 v) => new(v.X, v.Y, v.Z);

    private static Vector3 ToGodot(NVector3 v) => new(v.X, v.Y, v.Z);
}

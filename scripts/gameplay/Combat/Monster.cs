using Godot;
using Ragna.Core.Combat;
using Ragna.Gameplay.Characters;

namespace Ragna.Gameplay.Combat;

/// <summary>
/// Something the player can click to attack. It has combat stats and HP, flashes when hit, and
/// disappears when it dies. It sits on the <c>monsters</c> physics layer (layer 3) so the player's
/// click ray can find it. Movement, AI and respawning come with GAM-11.
/// </summary>
public partial class Monster : CharacterBody3D
{
    /// <summary>Physics layer bit of the <c>monsters</c> layer (layer 3).</summary>
    public const uint MonsterLayer = 1u << 2;

    private static readonly Color HitFlash = new(1f, 0.45f, 0.45f);

    private CharacterSprite? _sprite;
    private bool _grounded;

    /// <summary>Raised when an attack lands or misses, with the attacker. For damage numbers (GAM-12) and logs.</summary>
    public event Action<AttackResult, Node3D>? Attacked;

    /// <summary>Raised once when HP reaches 0, with the killer.</summary>
    public event Action<Monster, Node3D>? Died;

    [Export]
    public string DisplayName { get; set; } = "Monster";

    [Export(PropertyHint.Range, "1,99,1")]
    public int Level { get; set; } = 1;

    [Export(PropertyHint.Range, "1,100000,1")]
    public int MaxHp { get; set; } = 60;

    [Export(PropertyHint.Range, "0,10000,1")]
    public int Atk { get; set; } = 8;

    [Export(PropertyHint.Range, "0,10000,1")]
    public int Def { get; set; }

    [Export(PropertyHint.Range, "0,10000,1")]
    public int Hit { get; set; } = 10;

    [Export(PropertyHint.Range, "0,10000,1")]
    public int Flee { get; set; } = 2;

    /// <summary>EXP the killer gets.</summary>
    [Export(PropertyHint.Range, "0,1000000,1")]
    public int ExpReward { get; set; } = 30;

    /// <summary>Seconds the body stays on screen after dying.</summary>
    [Export(PropertyHint.Range, "0,10,0.1,suffix:s")]
    public float CorpseTime { get; set; } = 0.6f;

    /// <summary>Tint of the sprite, so placeholder monsters don't look like the player.</summary>
    [Export]
    public Color Tint { get; set; } = Colors.White;

    public CombatStats CombatStats { get; private set; } = new(1, 0, 0, 0, 0, 0);

    public Health Health { get; private set; } = new(1);

    public bool IsDead => Health.IsDead;

    public override void _Ready()
    {
        CombatStats = new CombatStats(Level, Atk, Def, Hit, Flee, CritChance: 0);
        Health = new Health(MaxHp);
        _sprite = FindSprite(this);
        if (_sprite is not null)
        {
            _sprite.Modulate = Tint;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Settle on the ground once, after the physics space has the terrain in it.
        if (_grounded)
        {
            return;
        }

        _grounded = true;
        var from = GlobalPosition + (Vector3.Up * 20f);
        var query = PhysicsRayQueryParameters3D.Create(from, GlobalPosition + (Vector3.Down * 20f), 1, [GetRid()]);
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count > 0)
        {
            GlobalPosition = hit["position"].AsVector3();
        }
    }

    /// <summary>Applies one attack from <paramref name="attacker"/>. Does nothing once dead.</summary>
    public void ReceiveAttack(AttackResult result, Node3D attacker)
    {
        if (IsDead)
        {
            return;
        }

        Health.TakeDamage(result.Damage);
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

    private void Die(Node3D killer)
    {
        // Stop being clickable straight away.
        CollisionLayer = 0;
        Died?.Invoke(this, killer);

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

    private void Flash()
    {
        if (_sprite is null)
        {
            return;
        }

        _sprite.Modulate = HitFlash * Tint;
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", Tint, 0.2f);
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
}

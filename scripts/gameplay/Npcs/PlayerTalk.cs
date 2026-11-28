using Godot;
using Ragna.Core.Items;
using Ragna.Dialogue;
using Ragna.Gameplay.Combat;
using Ragna.Gameplay.Items;
using Ragna.Gameplay.Movement;
using NVector3 = System.Numerics.Vector3;

namespace Ragna.Gameplay.Npcs;

/// <summary>
/// Click-to-talk for the player. Left-click an <see cref="Npc"/>: the player walks to it along the
/// navmesh and, once within <see cref="Reach"/>, the two face each other and the NPC's dialogue scene
/// plays. Clicking the ground, a monster or an item drops the order.
/// </summary>
/// <remarks>
/// Put it as a child of the player's <see cref="ClickToMoveController"/>, after the
/// <see cref="PlayerPickup"/> node. Godot sends unhandled input to the last child first, so a click
/// on an NPC is taken here and reaches neither pickup, combat nor the mover; any other click goes on.
/// The walk uses the same rules as walking to an item (<see cref="PickupOrder"/>): NPCs stand still.
/// </remarks>
public partial class PlayerTalk : Node
{
    private const float PickRayLength = 1000f;

    private PickupOrder _order = new();
    private ClickToMoveController _body = null!;
    private PlayerCombat? _combat;
    private PlayerPickup? _pickup;
    private Npc? _target;

    /// <summary>Raised when a talk starts, with the NPC.</summary>
    public event Action<Npc>? Talked;

    /// <summary>Ground distance from the player's centre at which the player can talk to an NPC.</summary>
    [Export(PropertyHint.Range, "0.5,5,0.05,suffix:m")]
    public float Reach { get; set; } = 1.6f;

    /// <summary>Physics layers the click ray can hit. Terrain (1), monsters (3) and items (4) block it; NPCs (5) are targets.</summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint PickMask { get; set; } = 1u | Monster.MonsterLayer | GroundItem.ItemLayer | Npc.NpcLayer;

    /// <summary>The NPC being walked to, if any.</summary>
    public Npc? Target => _target;

    public override void _Ready()
    {
        _body = GetParent() as ClickToMoveController
            ?? throw new InvalidOperationException($"{Name} must be a child of a ClickToMoveController.");
        _order = new PickupOrder(Reach);
        _combat = _body.GetChildren().OfType<PlayerCombat>().FirstOrDefault();
        _pickup = _body.GetChildren().OfType<PlayerPickup>().FirstOrDefault();
        if (_combat is not null)
        {
            // Attacking a monster or dying ends the walk to an NPC.
            _combat.TargetChanged += monster =>
            {
                if (monster is not null)
                {
                    Cancel();
                }
            };
            _combat.Died += _ => Cancel();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed(ClickToMoveController.MoveAction) || _combat is { IsDead: true })
        {
            return;
        }

        var screenPoint = @event is InputEventMouse mouse ? mouse.Position : GetViewport().GetMousePosition();
        if (PickNpc(screenPoint) is { } npc)
        {
            _combat?.StopAttacking();
            _pickup?.Cancel();
            _target = npc;
            _order.Start();
            GetViewport().SetInputAsHandled();
        }
        else
        {
            // The ground, a monster or an item: let the others have it.
            Cancel();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_order.IsActive)
        {
            return;
        }

        if (_target is null || !IsInstanceValid(_target))
        {
            Cancel();
            return;
        }

        var npcPosition = _target.GlobalPosition;
        var step = _order.Tick(ToCore(_body.GlobalPosition), ToCore(npcPosition), _body.Mover.Destination is not null);
        switch (step.Action)
        {
            case PickupAction.Walk when step.Repath:
                _body.MoveTo(npcPosition);
                break;
            case PickupAction.PickUp:
                _body.StopMoving();
                StartTalk();
                break;
            case PickupAction.GiveUp:
                GD.Print($"Can't reach {_target.DisplayName}.");
                _target = null;
                break;
        }
    }

    /// <summary>Drops the walk to an NPC, if any.</summary>
    public void Cancel()
    {
        _order.Cancel();
        _target = null;
    }

    private void StartTalk()
    {
        var npc = _target!;
        _target = null;
        if (DialogueScene.Instance is not { IsPlaying: false } scene)
        {
            return;
        }

        npc.FaceTowards(_body.GlobalPosition);
        _body.GetNodeOrNull<Characters.CharacterSprite>("Visual/Sprite")?.Face(npc.GlobalPosition - _body.GlobalPosition);
        if (scene.Play(npc.DialogueId))
        {
            Talked?.Invoke(npc);
        }
    }

    /// <summary>The NPC under <paramref name="screenPoint"/>, unless terrain, a monster or an item is in front of it.</summary>
    private Npc? PickNpc(Vector2 screenPoint)
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
        return hit.Count > 0 && hit["collider"].AsGodotObject() is Npc npc ? npc : null;
    }

    private static NVector3 ToCore(Vector3 v) => new(v.X, v.Y, v.Z);
}

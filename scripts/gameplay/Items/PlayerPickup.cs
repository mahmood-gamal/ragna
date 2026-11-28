using Godot;
using Ragna.Core.Items;
using Ragna.Gameplay.Combat;
using Ragna.Gameplay.Movement;
using NVector3 = System.Numerics.Vector3;

namespace Ragna.Gameplay.Items;

/// <summary>
/// Click-to-pick-up for the player. Left-click an item on the ground: the player walks to it along
/// the navmesh and picks it up once within <see cref="Reach"/>. Clicking the ground or a monster
/// drops the order. The rules live in <see cref="PickupOrder"/>.
/// </summary>
/// <remarks>
/// Put it as a child of the player's <see cref="ClickToMoveController"/>, after the
/// <see cref="PlayerCombat"/> node. Godot sends unhandled input to the last child first, so a click
/// on an item is taken here and reaches neither combat nor the mover; any other click goes on.
/// </remarks>
public partial class PlayerPickup : Node
{
    private const float PickRayLength = 1000f;

    private PickupOrder _order = new();
    private ClickToMoveController _body = null!;
    private PlayerCombat? _combat;
    private GroundItem? _target;

    /// <summary>
    /// Raised when the player picks something up. The inventory (GAM-14) listens to this.
    /// </summary>
    public event Action<ItemDefinition, int>? PickedUp;

    /// <summary>Ground distance from the player's centre at which an item can be picked up.</summary>
    [Export(PropertyHint.Range, "0.2,5,0.05,suffix:m")]
    public float Reach { get; set; } = 0.9f;

    /// <summary>Physics layers the click ray can hit. Terrain (1) and monsters (3) block it; items (4) are targets.</summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint PickMask { get; set; } = 1u | Monster.MonsterLayer | GroundItem.ItemLayer;

    /// <summary>The item being walked to, if any.</summary>
    public GroundItem? Target => _target;

    public override void _Ready()
    {
        _body = GetParent() as ClickToMoveController
            ?? throw new InvalidOperationException($"{Name} must be a child of a ClickToMoveController.");
        _order = new PickupOrder(Reach);
        _combat = _body.GetChildren().OfType<PlayerCombat>().FirstOrDefault();
        if (_combat is not null)
        {
            // Attacking a monster or dying ends the walk to an item.
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
        if (PickItem(screenPoint) is { } item)
        {
            _combat?.StopAttacking();
            _target = item;
            _order.Start();
            GetViewport().SetInputAsHandled();
        }
        else
        {
            // The ground or a monster: let combat and the mover have it.
            Cancel();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_order.IsActive)
        {
            return;
        }

        if (_target is null || !IsInstanceValid(_target) || _target.IsTaken)
        {
            // Someone else took it, or it vanished.
            Cancel();
            return;
        }

        var itemPosition = _target.RestPosition;
        var step = _order.Tick(ToCore(_body.GlobalPosition), ToCore(itemPosition), _body.Mover.Destination is not null);
        switch (step.Action)
        {
            case PickupAction.Walk when step.Repath:
                _body.MoveTo(itemPosition);
                break;
            case PickupAction.PickUp:
                _body.StopMoving();
                TakeTarget();
                break;
            case PickupAction.GiveUp:
                GD.Print($"Can't reach {_target.Caption}.");
                _target = null;
                break;
        }
    }

    private void TakeTarget()
    {
        var item = _target!;
        _target = null;
        if (!item.PickUp())
        {
            return;
        }

        PickedUp?.Invoke(item.Item, item.Quantity);
        GD.Print($"Picked up {item.Caption}.");
    }

    private void Cancel()
    {
        _order.Cancel();
        _target = null;
    }

    /// <summary>The item under <paramref name="screenPoint"/>, unless terrain or a monster is in front of it.</summary>
    private GroundItem? PickItem(Vector2 screenPoint)
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
        return hit.Count > 0 && hit["collider"].AsGodotObject() is GroundItem { IsTaken: false } item ? item : null;
    }

    private static NVector3 ToCore(Vector3 v) => new(v.X, v.Y, v.Z);
}

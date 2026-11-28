using Godot;
using Ragna.Core.Movement;
using NVector3 = System.Numerics.Vector3;

namespace Ragna.Gameplay.Movement;

/// <summary>
/// Ragnarok-style click-to-move. Left-click the ground to walk there along the navmesh; hold the
/// button to keep walking towards the cursor. Needs a <see cref="NavigationAgent3D"/> child and a
/// <see cref="NavigationRegion3D"/> with a baked navmesh in the scene (see docs/navigation.md).
/// The rules live in <see cref="ClickToMove"/>.
/// </summary>
/// <remarks>
/// The body walks the path directly instead of using <c>MoveAndSlide</c>: the navmesh already keeps
/// it off cliffs and water, and a capsule would snag on the open sides of ramps. Each step it is
/// put back on the ground with a short ray down. It stays a <see cref="CharacterBody3D"/> so other
/// bodies and picking rays can hit it.
/// </remarks>
public partial class ClickToMoveController : CharacterBody3D
{
    public const string MoveAction = "move";

    private const float PickRayLength = 1000f;

    // How far above and below its feet the body looks for the ground after a step.
    private const float GroundProbeUp = 0.6f;
    private const float GroundProbeDown = 1.5f;

    private ClickToMove _mover = new();
    private NavigationAgent3D _agent = null!;

    /// <summary>Camera used to turn the cursor into a ray. Defaults to the viewport's current camera.</summary>
    [Export]
    public Camera3D? Camera { get; set; }

    /// <summary>The agent that finds paths. Defaults to the first <see cref="NavigationAgent3D"/> child.</summary>
    [Export]
    public NavigationAgent3D? Agent { get; set; }

    [Export(PropertyHint.Range, "0.5,20,0.1,suffix:m/s")]
    public float MoveSpeed { get; set; } = 5f;

    /// <summary>While the button is held, the shortest time between two new paths.</summary>
    [Export(PropertyHint.Range, "0,1,0.01,suffix:s")]
    public float RepathInterval { get; set; } = 0.1f;

    /// <summary>While the button is held, how far the cursor point must move before a new path is asked for.</summary>
    [Export(PropertyHint.Range, "0,5,0.05,suffix:m")]
    public float RetargetDistance { get; set; } = 0.25f;

    /// <summary>Physics layers the cursor ray and the ground probe can hit. Layer 1 is the terrain.</summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint GroundMask { get; set; } = 1;

    /// <summary>The click-to-move state, for code that needs the current destination.</summary>
    public ClickToMove Mover => _mover;

    public override void _Ready()
    {
        _mover = new ClickToMove(new ClickToMoveSettings
        {
            MoveSpeed = MoveSpeed,
            RepathInterval = RepathInterval,
            RetargetDistance = RetargetDistance,
        });
        _agent = Agent ?? GetChildren().OfType<NavigationAgent3D>().FirstOrDefault()
            ?? throw new InvalidOperationException($"{Name} needs a NavigationAgent3D child.");

        // Stand still until the first click; the agent targets its own position by default.
        _agent.TargetPosition = GlobalPosition;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(MoveAction))
        {
            var screenPoint = @event is InputEventMouse mouse ? mouse.Position : GetViewport().GetMousePosition();
            if (PickGround(screenPoint) is { } point && _mover.Press(ToCore(point)))
            {
                _agent.TargetPosition = point;
            }

            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionReleased(MoveAction))
        {
            _mover.Release();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = (float)delta;

        if (_mover.IsHolding)
        {
            // Catch a release that happened over a GUI control or outside the window.
            if (!Input.IsActionPressed(MoveAction))
            {
                _mover.Release();
            }
            else
            {
                var cursor = PickGround(GetViewport().GetMousePosition());
                if (_mover.Hold(cursor is { } c ? ToCore(c) : null, dt) && _mover.Destination is { } d)
                {
                    _agent.TargetPosition = ToGodot(d);
                }
            }
        }

        if (_mover.Destination is null || dt <= 0f)
        {
            Velocity = Vector3.Zero;
            return;
        }

        if (_agent.IsNavigationFinished())
        {
            _mover.Arrive();
            Velocity = Vector3.Zero;
            return;
        }

        var next = _agent.GetNextPathPosition();
        Velocity = ToGodot(ClickToMove.HorizontalVelocity(ToCore(GlobalPosition), ToCore(next), _mover.Settings.MoveSpeed, dt));
        var position = GlobalPosition + (Velocity * dt);
        if (GroundHeight(position) is { } y)
        {
            position.Y = y;
        }

        GlobalPosition = position;
    }

    /// <summary>
    /// Walks to <paramref name="point"/> along the navmesh as if the ground there was clicked once,
    /// for example to chase an attack target. Ends any hold of the move button.
    /// </summary>
    public void MoveTo(Vector3 point)
    {
        _mover.MoveTo(ToCore(point));
        _agent.TargetPosition = point;
    }

    /// <summary>Stops walking where the character stands, for example to attack.</summary>
    public void StopMoving()
    {
        _mover.Stop();
        _agent.TargetPosition = GlobalPosition;
        Velocity = Vector3.Zero;
    }

    /// <summary>Puts the character at <paramref name="point"/> standing still, for example when loading a save.</summary>
    public void Teleport(Vector3 point)
    {
        GlobalPosition = point;
        StopMoving();
    }

    /// <summary>Where the ray through <paramref name="screenPoint"/> hits the ground, if anywhere.</summary>
    public Vector3? PickGround(Vector2 screenPoint)
    {
        var camera = Camera ?? GetViewport().GetCamera3D();
        if (camera is null)
        {
            return null;
        }

        var from = camera.ProjectRayOrigin(screenPoint);
        return CastGround(from, from + (camera.ProjectRayNormal(screenPoint) * PickRayLength));
    }

    private float? GroundHeight(Vector3 feet) =>
        CastGround(feet + (Vector3.Up * GroundProbeUp), feet + (Vector3.Down * GroundProbeDown))?.Y;

    private Vector3? CastGround(Vector3 from, Vector3 to)
    {
        var query = PhysicsRayQueryParameters3D.Create(from, to, GroundMask, [GetRid()]);
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        return hit.Count > 0 ? hit["position"].AsVector3() : null;
    }

    private static NVector3 ToCore(Vector3 v) => new(v.X, v.Y, v.Z);

    private static Vector3 ToGodot(NVector3 v) => new(v.X, v.Y, v.Z);
}

using System.Numerics;

namespace Ragna.Core.Movement;

/// <summary>Tuning for <see cref="ClickToMove"/>. Distances are in metres, times in seconds.</summary>
public sealed record ClickToMoveSettings
{
    /// <summary>Walking speed along the path.</summary>
    public float MoveSpeed { get; init; } = 5f;

    /// <summary>While the button is held, the shortest time between two new paths.</summary>
    public float RepathInterval { get; init; } = 0.1f;

    /// <summary>While the button is held, how far the cursor point must move before a new path is asked for.</summary>
    public float RetargetDistance { get; init; } = 0.25f;
}

/// <summary>
/// Engine-agnostic click-to-move rules, Ragnarok style: click the ground to walk there,
/// hold the button to keep walking towards the cursor. The engine node picks the ground
/// point under the cursor, asks for a path whenever a method here returns <c>true</c>,
/// and moves the character with <see cref="HorizontalVelocity"/>.
/// </summary>
public sealed class ClickToMove
{
    private float _sinceRepath;

    public ClickToMove(ClickToMoveSettings? settings = null)
    {
        Settings = settings ?? new ClickToMoveSettings();
        if (Settings.MoveSpeed <= 0f)
        {
            throw new ArgumentException("MoveSpeed must be above 0.", nameof(settings));
        }

        if (Settings.RepathInterval < 0f || Settings.RetargetDistance < 0f)
        {
            throw new ArgumentException("RepathInterval and RetargetDistance can't be negative.", nameof(settings));
        }
    }

    public ClickToMoveSettings Settings { get; }

    /// <summary>The ground point the character is walking to, or <c>null</c> when it stands still.</summary>
    public Vector3? Destination { get; private set; }

    /// <summary>Whether the move button is held down after a click on the ground.</summary>
    public bool IsHolding { get; private set; }

    /// <summary>
    /// The move button went down on the ground at <paramref name="point"/>.
    /// Always returns <c>true</c>: a click always asks for a new path.
    /// </summary>
    public bool Press(Vector3 point)
    {
        IsHolding = true;
        Destination = point;
        _sinceRepath = 0f;
        return true;
    }

    /// <summary>
    /// Called every physics frame while the button is held. <paramref name="point"/> is the ground
    /// under the cursor, or <c>null</c> when the cursor isn't over the ground (the last destination is kept).
    /// Returns <c>true</c> when the destination changed and a new path is needed.
    /// </summary>
    public bool Hold(Vector3? point, float delta)
    {
        if (!IsHolding)
        {
            return false;
        }

        _sinceRepath += delta;
        if (point is not { } p || _sinceRepath < Settings.RepathInterval)
        {
            return false;
        }

        if (Destination is { } current && Vector3.Distance(current, p) < Settings.RetargetDistance)
        {
            return false;
        }

        Destination = p;
        _sinceRepath = 0f;
        return true;
    }

    /// <summary>The move button was released. The character still walks to the last destination.</summary>
    public void Release() => IsHolding = false;

    /// <summary>
    /// Walks to <paramref name="point"/> without holding the button, for example when chasing an
    /// attack target. Ends any hold. The caller asks for the path.
    /// </summary>
    public void MoveTo(Vector3 point)
    {
        IsHolding = false;
        Destination = point;
        _sinceRepath = 0f;
    }

    /// <summary>Stops walking and ends any hold, for example when an attack target comes into range.</summary>
    public void Stop()
    {
        IsHolding = false;
        Destination = null;
    }

    /// <summary>The character reached the end of its path.</summary>
    public void Arrive()
    {
        if (!IsHolding)
        {
            Destination = null;
        }
    }

    /// <summary>
    /// Ground-plane velocity that walks from <paramref name="position"/> towards <paramref name="waypoint"/>
    /// at <paramref name="speed"/>, slowing down so one step of <paramref name="delta"/> seconds never
    /// overshoots. Y is always 0; gravity and slopes are left to the physics body.
    /// </summary>
    public static Vector3 HorizontalVelocity(Vector3 position, Vector3 waypoint, float speed, float delta)
    {
        var offset = new Vector3(waypoint.X - position.X, 0f, waypoint.Z - position.Z);
        var distance = offset.Length();
        if (distance < 1e-4f || speed <= 0f)
        {
            return Vector3.Zero;
        }

        var stepSpeed = delta > 0f ? MathF.Min(speed, distance / delta) : speed;
        return offset / distance * stepSpeed;
    }
}

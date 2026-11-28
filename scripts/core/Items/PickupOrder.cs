using System.Numerics;

namespace Ragna.Core.Items;

public enum PickupAction
{
    /// <summary>No item ordered: do nothing.</summary>
    None,

    /// <summary>Out of reach: keep walking towards the item.</summary>
    Walk,

    /// <summary>In reach: stop and pick the item up now. The order ends.</summary>
    PickUp,

    /// <summary>The walk ended out of reach (no path gets closer). The order ends.</summary>
    GiveUp,
}

/// <summary>What the player should do this frame.</summary>
/// <param name="Action">What to do.</param>
/// <param name="Repath">With <see cref="PickupAction.Walk"/>: ask for a path to the item.</param>
public readonly record struct PickupStep(PickupAction Action, bool Repath = false);

/// <summary>
/// Click-to-pick-up: after an item on the ground is clicked, walk to it and pick it up once within
/// <see cref="Reach"/>. The engine node calls <see cref="Tick"/> every physics frame and acts on the
/// step it returns. Items don't move, so the path is asked for once.
/// </summary>
public sealed class PickupOrder
{
    private bool _needsPath;

    /// <param name="reach">Ground distance from the player's centre at which an item can be picked up.</param>
    public PickupOrder(float reach = 1f)
    {
        if (!(reach > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(reach), "Reach must be above 0.");
        }

        Reach = reach;
    }

    public float Reach { get; }

    /// <summary>Whether an item is ordered.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Orders a walk to a new item. The next <see cref="Tick"/> asks for a path.</summary>
    public void Start()
    {
        IsActive = true;
        _needsPath = true;
    }

    /// <summary>Drops the order, for example when the ground or a monster is clicked.</summary>
    public void Cancel()
    {
        IsActive = false;
        _needsPath = false;
    }

    /// <param name="position">The player's feet.</param>
    /// <param name="item">The item's position.</param>
    /// <param name="walking">Whether the player is still walking the path from the last repath.</param>
    public PickupStep Tick(Vector3 position, Vector3 item, bool walking)
    {
        if (!IsActive)
        {
            return new PickupStep(PickupAction.None);
        }

        if (GroundDistance(position, item) <= Reach)
        {
            Cancel();
            return new PickupStep(PickupAction.PickUp);
        }

        if (_needsPath)
        {
            _needsPath = false;
            return new PickupStep(PickupAction.Walk, Repath: true);
        }

        if (!walking)
        {
            Cancel();
            return new PickupStep(PickupAction.GiveUp);
        }

        return new PickupStep(PickupAction.Walk);
    }

    /// <summary>Distance between <paramref name="a"/> and <paramref name="b"/> ignoring height.</summary>
    public static float GroundDistance(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();
}

using Godot;
using Ragna.Core.Combat;
using Ragna.Core.Items;
using Ragna.Core.Monsters;

namespace Ragna.Gameplay.Items;

/// <summary>Rolls a dead monster's drop table and throws the items onto the ground around it.</summary>
public static class LootDropper
{
    /// <summary>Physics layer of the terrain the items land on.</summary>
    public const uint GroundMask = 1;

    public const string DefaultScenePath = "res://scenes/items/GroundItem.tscn";

    private const float MinScatter = 0.35f;
    private const float MaxScatter = 0.9f;

    /// <summary>
    /// Rolls <paramref name="monster"/>'s drops and spawns a <see cref="GroundItem"/> for each, under
    /// <paramref name="parent"/>, falling from <paramref name="at"/> to spots on the navmesh around it.
    /// </summary>
    public static IReadOnlyList<GroundItem> Drop(MonsterDefinition monster, Node3D parent, Vector3 at, PackedScene? scene = null)
    {
        var drops = DropTable.Roll(monster.Drops, SystemRandomSource.Shared);
        if (drops.Count == 0)
        {
            return [];
        }

        scene ??= GD.Load<PackedScene>(DefaultScenePath);
        var offsets = DropScatter.Offsets(drops.Count, MinScatter, MaxScatter, SystemRandomSource.Shared);
        var map = parent.GetWorld3D().NavigationMap;
        var spawned = new List<GroundItem>(drops.Count);
        for (var i = 0; i < drops.Count; i++)
        {
            if (!ItemData.Database.TryGet(drops[i].Item, out var item))
            {
                GD.PushWarning($"{monster.Id} dropped unknown item '{drops[i].Item}'; skipped.");
                continue;
            }

            if (scene.Instantiate() is not GroundItem groundItem)
            {
                GD.PushError($"{scene.ResourcePath}'s root is not a GroundItem.");
                return spawned;
            }

            var offset = offsets[i];
            var landing = NavigationServer3D.MapGetClosestPoint(map, at + new Vector3(offset.X, 0f, offset.Z));
            landing.Y = GroundHeight(parent, landing) ?? landing.Y;
            groundItem.Setup(item, drops[i].Quantity);
            parent.AddChild(groundItem);
            groundItem.Fall(at + (Vector3.Up * 0.5f), landing);
            spawned.Add(groundItem);
        }

        return spawned;
    }

    // The navmesh floats a little above the terrain, so put the item on the terrain itself.
    private static float? GroundHeight(Node3D node, Vector3 point)
    {
        var query = PhysicsRayQueryParameters3D.Create(point + (Vector3.Up * 2f), point + (Vector3.Down * 3f), GroundMask);
        var hit = node.GetWorld3D().DirectSpaceState.IntersectRay(query);
        return hit.Count > 0 ? hit["position"].AsVector3().Y : null;
    }
}

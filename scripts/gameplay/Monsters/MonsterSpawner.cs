using Godot;
using Ragna.Core.Monsters;
using Ragna.Gameplay.Combat;

namespace Ragna.Gameplay.Monsters;

/// <summary>
/// A spawn point: keeps <see cref="Count"/> monsters of one type alive within <see cref="Radius"/>
/// of itself. When one dies, a new one spawns after the type's <c>respawnTime</c> (or
/// <see cref="RespawnTimeOverride"/>). Each monster wanders around, and leashes back to, the spot it
/// spawned on. The bookkeeping lives in <see cref="SpawnTimer"/>.
/// </summary>
public partial class MonsterSpawner : Node3D
{
    private SpawnTimer? _timer;
    private readonly RandomNumberGenerator _rng = new();

    /// <summary>Raised after a monster is spawned and added to the tree.</summary>
    public event Action<Monster>? Spawned;

    /// <summary>The scene to spawn. Its root must be a <see cref="Monster"/>.</summary>
    [Export]
    public PackedScene? MonsterScene { get; set; }

    /// <summary>Monster type in <c>data/monsters/monsters.json</c>.</summary>
    [Export]
    public string MonsterId { get; set; } = "slime";

    /// <summary>How many monsters this spawn point keeps alive.</summary>
    [Export(PropertyHint.Range, "1,50,1")]
    public int Count { get; set; } = 1;

    /// <summary>Monsters spawn at random points on the navmesh within this distance.</summary>
    [Export(PropertyHint.Range, "0,50,0.5,suffix:m")]
    public float Radius { get; set; } = 3f;

    /// <summary>Seconds before a dead monster comes back. Below 0, the type's <c>respawnTime</c> is used.</summary>
    [Export(PropertyHint.Range, "-1,600,0.5,suffix:s")]
    public float RespawnTimeOverride { get; set; } = -1f;

    /// <summary>Monsters from this spawn point that are alive now.</summary>
    public int Alive => _timer?.Alive ?? 0;

    /// <summary>Seconds until the next respawn, or <c>null</c> when every monster is alive.</summary>
    public double? NextSpawnIn => _timer?.NextSpawnIn;

    public override void _Ready()
    {
        var respawn = RespawnTimeOverride >= 0f ? RespawnTimeOverride : MonsterData.Get(MonsterId).RespawnTime;
        _timer = new SpawnTimer(Count, respawn);
        if (MonsterScene is null)
        {
            GD.PushWarning($"{Name} has no MonsterScene; it won't spawn anything.");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Spawn points need the navmesh, which is ready after the first map sync.
        if (_timer is null || MonsterScene is null || NavigationServer3D.MapGetIterationId(GetWorld3D().NavigationMap) == 0)
        {
            return;
        }

        for (var due = _timer.Tick(delta); due > 0; due--)
        {
            Spawn();
        }
    }

    private void Spawn()
    {
        if (MonsterScene!.Instantiate() is not Monster monster)
        {
            GD.PushError($"{Name}: MonsterScene's root is not a Monster.");
            return;
        }

        var home = RandomPointOnNavmesh();
        monster.MonsterId = MonsterId;
        monster.Home = home;
        monster.Position = ToLocal(home);
        monster.Died += OnMonsterDied;
        AddChild(monster);
        Spawned?.Invoke(monster);
    }

    private void OnMonsterDied(Monster monster, Node3D killer) => _timer?.NotifyDied();

    private Vector3 RandomPointOnNavmesh()
    {
        var angle = _rng.RandfRange(0f, Mathf.Tau);
        var distance = Radius * Mathf.Sqrt(_rng.Randf());
        var point = GlobalPosition + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        return NavigationServer3D.MapGetClosestPoint(GetWorld3D().NavigationMap, point);
    }
}

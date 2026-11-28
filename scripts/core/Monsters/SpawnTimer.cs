namespace Ragna.Core.Monsters;

/// <summary>
/// Respawn bookkeeping for one spawn point that keeps up to <see cref="Capacity"/> monsters alive.
/// All of them are due at once when the timer is made; after that, each death makes one more due
/// <see cref="RespawnDelay"/> seconds later. The engine node calls <see cref="Tick"/> every frame,
/// spawns as many as it returns, and calls <see cref="NotifyDied"/> when one dies.
/// </summary>
public sealed class SpawnTimer
{
    private readonly List<double> _pending = [];

    public SpawnTimer(int capacity, double respawnDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(respawnDelay);
        Capacity = capacity;
        RespawnDelay = respawnDelay;
        for (var i = 0; i < capacity; i++)
        {
            _pending.Add(0);
        }
    }

    public int Capacity { get; }

    public double RespawnDelay { get; }

    /// <summary>Monsters spawned and not yet dead.</summary>
    public int Alive { get; private set; }

    /// <summary>Monsters waiting to respawn.</summary>
    public int Pending => _pending.Count;

    /// <summary>Seconds until the next monster is due, or <c>null</c> when none is waiting.</summary>
    public double? NextSpawnIn => _pending.Count == 0 ? null : _pending.Min();

    /// <summary>Advances by <paramref name="delta"/> seconds. Returns how many monsters to spawn now.</summary>
    public int Tick(double delta)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delta);
        var due = 0;
        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            _pending[i] -= delta;
            if (_pending[i] <= 0)
            {
                _pending.RemoveAt(i);
                due++;
            }
        }

        Alive += due;
        return due;
    }

    /// <summary>One of this spawn point's monsters died: queue its respawn.</summary>
    /// <exception cref="InvalidOperationException">No monster is alive.</exception>
    public void NotifyDied()
    {
        if (Alive == 0)
        {
            throw new InvalidOperationException("No monster from this spawn point is alive.");
        }

        Alive--;
        _pending.Add(RespawnDelay);
    }
}

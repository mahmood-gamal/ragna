using Ragna.Core.Monsters;

namespace Ragna.Core.Tests.Monsters;

public sealed class SpawnTimerTests
{
    [Fact]
    public void Spawns_every_monster_on_the_first_tick()
    {
        var timer = new SpawnTimer(3, 10);

        Assert.Equal(3, timer.Tick(0));
        Assert.Equal(3, timer.Alive);
        Assert.Equal(0, timer.Pending);
        Assert.Equal(0, timer.Tick(1));
    }

    [Fact]
    public void Respawns_after_the_delay()
    {
        var timer = new SpawnTimer(2, 10);
        timer.Tick(0);

        timer.NotifyDied();

        Assert.Equal(1, timer.Alive);
        Assert.Equal(10, timer.NextSpawnIn);
        Assert.Equal(0, timer.Tick(9.9));
        Assert.Equal(1, timer.Tick(0.1));
        Assert.Equal(2, timer.Alive);
        Assert.Null(timer.NextSpawnIn);
    }

    [Fact]
    public void Each_death_has_its_own_timer()
    {
        var timer = new SpawnTimer(2, 10);
        timer.Tick(0);

        timer.NotifyDied();
        timer.Tick(4);
        timer.NotifyDied();

        Assert.Equal(1, timer.Tick(6));
        Assert.Equal(0, timer.Tick(3.5));
        Assert.Equal(1, timer.Tick(0.5));
    }

    [Fact]
    public void Zero_delay_respawns_on_the_next_tick()
    {
        var timer = new SpawnTimer(1, 0);
        timer.Tick(0);
        timer.NotifyDied();

        Assert.Equal(1, timer.Tick(0));
    }

    [Fact]
    public void Death_without_a_living_monster_throws()
    {
        var timer = new SpawnTimer(1, 5);

        Assert.Throws<InvalidOperationException>(timer.NotifyDied);
    }

    [Fact]
    public void Rejects_bad_settings()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpawnTimer(0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpawnTimer(1, -1));
    }
}

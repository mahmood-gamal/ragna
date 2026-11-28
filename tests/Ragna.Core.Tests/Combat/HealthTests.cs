using Ragna.Core.Combat;

namespace Ragna.Core.Tests.Combat;

public sealed class HealthTests
{
    [Fact]
    public void Starts_full()
    {
        var health = new Health(50);

        Assert.Equal(50, health.Current);
        Assert.Equal(1.0, health.Fraction);
        Assert.False(health.IsDead);
    }

    [Fact]
    public void Max_must_be_positive() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Health(0));

    [Fact]
    public void Damage_lowers_hp_and_raises_damaged()
    {
        var health = new Health(50);
        var damaged = new List<int>();
        health.Damaged += damaged.Add;

        Assert.Equal(20, health.TakeDamage(20));

        Assert.Equal(30, health.Current);
        Assert.Equal([20], damaged);
    }

    [Fact]
    public void Overkill_stops_at_zero_and_dies_once()
    {
        var health = new Health(50);
        var deaths = 0;
        health.Died += () => deaths++;

        Assert.Equal(50, health.TakeDamage(80));
        Assert.Equal(0, health.TakeDamage(10));

        Assert.True(health.IsDead);
        Assert.Equal(0, health.Current);
        Assert.Equal(1, deaths);
    }

    [Fact]
    public void Negative_damage_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Health(10).TakeDamage(-1));

    [Fact]
    public void Heal_is_capped_at_max_and_does_nothing_when_dead()
    {
        var health = new Health(50);
        health.TakeDamage(10);

        Assert.Equal(10, health.Heal(25));
        Assert.Equal(50, health.Current);

        health.TakeDamage(50);
        Assert.Equal(0, health.Heal(10));
    }

    [Fact]
    public void Changing_max_keeps_the_missing_hp()
    {
        var health = new Health(50);
        health.TakeDamage(10);

        health.SetMax(100);
        Assert.Equal(90, health.Current);

        health.SetMax(5);
        Assert.Equal(1, health.Current);
    }

    [Fact]
    public void Revive_restores_full_hp()
    {
        var health = new Health(50);
        health.TakeDamage(50);

        health.Revive();

        Assert.False(health.IsDead);
        Assert.Equal(50, health.Current);
    }

    [Fact]
    public void Changed_is_raised_for_every_change_to_hp_or_max()
    {
        var health = new Health(50);
        var changes = 0;
        health.Changed += () => changes++;

        health.TakeDamage(10);
        health.Heal(5);
        health.SetMax(60);
        health.TakeDamage(100);
        health.Revive();

        Assert.Equal(5, changes);
    }

    [Fact]
    public void Changed_is_not_raised_when_nothing_changes()
    {
        var health = new Health(50);
        var changes = 0;
        health.Changed += () => changes++;

        health.TakeDamage(0);
        health.Heal(10);

        Assert.Equal(0, changes);
    }
}

using Ragna.Core.Combat;

namespace Ragna.Core.Tests.Combat;

public sealed class SetCurrentTests
{
    [Theory]
    [InlineData(30, 30)]
    [InlineData(500, 100)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    public void Health_set_current_keeps_hp_between_1_and_max(int value, int expected)
    {
        var health = new Health(100);

        health.SetCurrent(value);

        Assert.Equal(expected, health.Current);
    }

    [Fact]
    public void Health_set_current_brings_the_dead_back()
    {
        var health = new Health(10);
        health.TakeDamage(10);

        health.SetCurrent(4);

        Assert.False(health.IsDead);
        Assert.Equal(4, health.Current);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(99, 20)]
    [InlineData(-1, 0)]
    public void Sp_set_current_keeps_sp_between_0_and_max(int value, int expected)
    {
        var sp = new SpPool(20);

        sp.SetCurrent(value);

        Assert.Equal(expected, sp.Current);
    }
}

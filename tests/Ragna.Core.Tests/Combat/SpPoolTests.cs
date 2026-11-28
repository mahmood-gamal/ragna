using Ragna.Core.Combat;

namespace Ragna.Core.Tests.Combat;

public sealed class SpPoolTests
{
    [Fact]
    public void Starts_full()
    {
        var sp = new SpPool(20);

        Assert.Equal(20, sp.Current);
        Assert.Equal(1.0, sp.Fraction);
    }

    [Fact]
    public void Zero_max_is_allowed_and_reads_empty()
    {
        var sp = new SpPool(0);

        Assert.Equal(0, sp.Current);
        Assert.Equal(0.0, sp.Fraction);
    }

    [Fact]
    public void Negative_max_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpPool(-1));

    [Fact]
    public void Spending_needs_enough_sp()
    {
        var sp = new SpPool(10);

        Assert.True(sp.TrySpend(7));
        Assert.False(sp.TrySpend(4));
        Assert.Equal(3, sp.Current);
    }

    [Fact]
    public void Restore_stops_at_max()
    {
        var sp = new SpPool(10);
        sp.TrySpend(6);

        Assert.Equal(6, sp.Restore(100));
        Assert.Equal(10, sp.Current);
    }

    [Fact]
    public void Changing_max_keeps_the_missing_sp()
    {
        var sp = new SpPool(10);
        sp.TrySpend(4);

        sp.SetMax(20);
        Assert.Equal(16, sp.Current);

        sp.SetMax(2);
        Assert.Equal(0, sp.Current);
    }

    [Fact]
    public void Refill_restores_full_sp()
    {
        var sp = new SpPool(10);
        sp.TrySpend(10);

        sp.Refill();

        Assert.Equal(10, sp.Current);
    }

    [Fact]
    public void Changed_is_raised_only_when_sp_changes()
    {
        var sp = new SpPool(10);
        var changes = 0;
        sp.Changed += () => changes++;

        sp.TrySpend(0);
        sp.TrySpend(50);
        sp.Restore(5);
        sp.TrySpend(3);
        sp.Restore(5);
        sp.SetMax(12);
        sp.Refill();

        Assert.Equal(4, changes);
    }
}

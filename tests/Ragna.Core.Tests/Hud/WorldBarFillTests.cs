using Ragna.Core.Combat;
using Ragna.Core.Hud;

namespace Ragna.Core.Tests.Hud;

public sealed class WorldBarFillTests
{
    [Fact]
    public void A_full_bar_fills_the_whole_width_centred()
    {
        Assert.Equal(new WorldBarFill(1, 0.8f, 0f), WorldBarFill.For(1, 0.8f));
    }

    [Fact]
    public void A_partial_bar_keeps_its_left_edge_in_place()
    {
        var fill = WorldBarFill.For(0.25, 1f);

        Assert.Equal(0.25f, fill.Width, 5);
        Assert.Equal(-0.375f, fill.CenterX, 5);

        // Left edge of the fill equals the left edge of the bar.
        Assert.Equal(-0.5f, fill.CenterX - (fill.Width / 2f), 5);
    }

    [Fact]
    public void An_empty_bar_has_no_width_at_the_left_edge()
    {
        Assert.Equal(new WorldBarFill(0, 0f, -0.5f), WorldBarFill.For(0, 1f));
    }

    [Theory]
    [InlineData(-0.5, 0)]
    [InlineData(1.5, 1)]
    [InlineData(double.NaN, 0)]
    public void Out_of_range_fractions_are_clamped(double fraction, double expected)
    {
        Assert.Equal(expected, WorldBarFill.For(fraction, 1f).Fraction);
    }

    [Fact]
    public void Follows_health()
    {
        var health = new Health(60);
        health.TakeDamage(45);

        var fill = WorldBarFill.For(health, 1f);

        Assert.Equal(0.25, fill.Fraction, 6);
        Assert.Equal(0.25f, fill.Width, 5);
    }

    [Fact]
    public void A_negative_width_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WorldBarFill.For(0.5, -1f));
    }
}

using Ragna.Core.Hud;

namespace Ragna.Core.Tests.Hud;

public sealed class FloatingNumberMotionTests
{
    private static readonly FloatingNumberMotion Motion = new();

    [Fact]
    public void Starts_where_it_spawned_fully_visible_and_popped()
    {
        var start = Motion.Sample(0f, 1);

        Assert.Equal(0f, start.OffsetX);
        Assert.Equal(0f, start.OffsetY);
        Assert.Equal(1f, start.Alpha);
        Assert.Equal(Motion.PopScale, start.Scale, 5);
    }

    [Fact]
    public void Ends_risen_drifted_and_invisible()
    {
        var end = Motion.Sample(Motion.Duration, 1);

        Assert.Equal(Motion.Rise, end.OffsetY, 5);
        Assert.Equal(Motion.Drift, end.OffsetX, 5);
        Assert.Equal(0f, end.Alpha, 5);
        Assert.Equal(1f, end.Scale, 5);
        Assert.True(Motion.IsFinished(Motion.Duration));
        Assert.False(Motion.IsFinished(Motion.Duration - 0.01f));
    }

    [Fact]
    public void Rises_steadily_and_slows_down()
    {
        var a = Motion.Sample(Motion.Duration * 0.25f, 0).OffsetY;
        var b = Motion.Sample(Motion.Duration * 0.5f, 0).OffsetY;
        var c = Motion.Sample(Motion.Duration * 0.75f, 0).OffsetY;

        Assert.True(a < b && b < c);
        Assert.True(b - a > c - b, "Each quarter should rise less than the one before.");
    }

    [Fact]
    public void Stays_opaque_until_the_fade_starts_then_fades()
    {
        Assert.Equal(1f, Motion.Sample(Motion.Duration * Motion.FadeStart * 0.99f, 0).Alpha);

        var halfway = Motion.FadeStart + ((1f - Motion.FadeStart) / 2f);
        Assert.Equal(0.5f, Motion.Sample(Motion.Duration * halfway, 0).Alpha, 4);
    }

    [Fact]
    public void Settles_to_normal_size_after_the_pop()
    {
        Assert.Equal(1f, Motion.Sample(Motion.PopTime, 0).Scale, 5);
        Assert.Equal(1f, Motion.Sample(Motion.Duration * 0.8f, 0).Scale, 5);
    }

    [Theory]
    [InlineData(-3, -1f)]
    [InlineData(0, 0f)]
    [InlineData(5, 1f)]
    public void Drift_side_follows_the_sign(int driftSign, float side)
    {
        Assert.Equal(Motion.Drift * side, Motion.Sample(Motion.Duration, driftSign).OffsetX, 5);
    }

    [Fact]
    public void Past_the_end_and_before_the_start_are_clamped()
    {
        Assert.Equal(Motion.Sample(Motion.Duration, 1), Motion.Sample(Motion.Duration * 3f, 1));
        Assert.Equal(Motion.Sample(0f, 1), Motion.Sample(-1f, 1));
    }

    [Fact]
    public void Zero_duration_and_pop_time_jump_straight_to_the_end()
    {
        var instant = new FloatingNumberMotion { Duration = 0f, PopTime = 0f };

        var sample = instant.Sample(0f, 1);

        Assert.Equal(instant.Rise, sample.OffsetY, 5);
        Assert.Equal(0f, sample.Alpha, 5);
        Assert.Equal(1f, sample.Scale, 5);
        Assert.True(instant.IsFinished(0f));
    }
}

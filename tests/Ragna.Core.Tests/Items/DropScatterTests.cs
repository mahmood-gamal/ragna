using Ragna.Core.Items;
using Ragna.Core.Tests.Combat;

namespace Ragna.Core.Tests.Items;

public sealed class DropScatterTests
{
    [Fact]
    public void Offsets_are_on_the_ground_within_the_radii()
    {
        var offsets = DropScatter.Offsets(5, 0.3f, 0.8f, new FixedRandom(0.2, 0.0, 0.5, 0.999, 0.7, 0.1));

        Assert.Equal(5, offsets.Count);
        foreach (var offset in offsets)
        {
            Assert.Equal(0f, offset.Y);
            Assert.InRange(offset.Length(), 0.3f - 1e-4f, 0.8f + 1e-4f);
        }
    }

    [Fact]
    public void Items_are_spread_evenly_around_the_circle()
    {
        var offsets = DropScatter.Offsets(4, 1f, 1f, new FixedRandom(0));

        Assert.Equal(1f, offsets[0].X, 4);
        Assert.Equal(1f, offsets[1].Z, 4);
        Assert.Equal(-1f, offsets[2].X, 4);
        Assert.Equal(-1f, offsets[3].Z, 4);
    }

    [Fact]
    public void No_items_no_offsets()
    {
        Assert.Empty(DropScatter.Offsets(0, 0.3f, 0.8f, new FixedRandom(0)));
    }

    [Theory]
    [InlineData(-1f, 1f)]
    [InlineData(1f, 0.5f)]
    public void Rejects_bad_radii(float min, float max)
    {
        Assert.Throws<ArgumentException>(() => DropScatter.Offsets(1, min, max, new FixedRandom(0)));
    }
}

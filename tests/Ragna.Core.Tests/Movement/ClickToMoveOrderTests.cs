using System.Numerics;
using Ragna.Core.Movement;

namespace Ragna.Core.Tests.Movement;

public sealed class ClickToMoveOrderTests
{
    private static readonly Vector3 A = new(1f, 0f, 1f);
    private static readonly Vector3 B = new(5f, 0f, 1f);

    [Fact]
    public void MoveTo_sets_the_destination_without_holding()
    {
        var mover = new ClickToMove();

        mover.MoveTo(A);

        Assert.Equal(A, mover.Destination);
        Assert.False(mover.IsHolding);
    }

    [Fact]
    public void MoveTo_ends_a_hold_so_the_cursor_no_longer_steers()
    {
        var mover = new ClickToMove();
        mover.Press(A);

        mover.MoveTo(B);

        Assert.False(mover.Hold(A, 1f));
        Assert.Equal(B, mover.Destination);
    }

    [Fact]
    public void Stop_clears_the_destination_and_the_hold()
    {
        var mover = new ClickToMove();
        mover.Press(A);

        mover.Stop();

        Assert.Null(mover.Destination);
        Assert.False(mover.IsHolding);
    }
}

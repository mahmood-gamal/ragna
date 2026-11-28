using Ragna.Core.Dialogue;

namespace Ragna.Core.Tests.Dialogue;

public sealed class StageSlotsTests
{
    [Fact]
    public void Every_slot_has_a_name_that_parses_back()
    {
        Assert.Equal(Enum.GetValues<StageSlot>().Length, StageSlots.Names.Count);
        var parsed = StageSlots.Names.Select(name => StageSlots.TryParse(name, out var slot) ? slot : (StageSlot?)null);

        Assert.Equal(Enum.GetValues<StageSlot>().Cast<StageSlot?>(), parsed);
        Assert.False(StageSlots.TryParse("Left", out _));
    }

    [Fact]
    public void Anchors_run_left_to_right_inside_the_screen()
    {
        var anchors = Enum.GetValues<StageSlot>().Select(StageSlots.Anchor).ToList();

        Assert.Equal(anchors.Order(), anchors);
        Assert.All(anchors, a => Assert.InRange(a, 0.05f, 0.95f));
        Assert.Equal(0.5f, StageSlots.Anchor(StageSlot.Center));
    }

    [Theory]
    [InlineData(StageSlot.FarLeft, false, false)]
    [InlineData(StageSlot.Left, false, false)]
    [InlineData(StageSlot.Center, false, false)]
    [InlineData(StageSlot.Right, false, true)]
    [InlineData(StageSlot.FarRight, false, true)]
    [InlineData(StageSlot.Left, true, true)]
    [InlineData(StageSlot.Right, true, false)]
    public void Right_side_portraits_face_the_middle_unless_flipped(StageSlot slot, bool flip, bool facesLeft)
    {
        Assert.Equal(facesLeft, StageSlots.FacesLeft(slot, flip));
    }

    [Theory]
    [InlineData(StageSlot.FarLeft, -1)]
    [InlineData(StageSlot.Center, -1)]
    [InlineData(StageSlot.Right, 1)]
    [InlineData(StageSlot.FarRight, 1)]
    public void Portraits_slide_in_from_their_own_side(StageSlot slot, int side)
    {
        Assert.Equal(side, StageSlots.EntrySide(slot));
    }
}

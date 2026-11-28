namespace Ragna.Core.Dialogue;

/// <summary>Names, positions and facing of <see cref="StageSlot"/>s.</summary>
public static class StageSlots
{
    private static readonly Dictionary<string, StageSlot> ByName = new(StringComparer.Ordinal)
    {
        ["far_left"] = StageSlot.FarLeft,
        ["left"] = StageSlot.Left,
        ["center"] = StageSlot.Center,
        ["right"] = StageSlot.Right,
        ["far_right"] = StageSlot.FarRight,
    };

    /// <summary>The slot names scripts use, left to right.</summary>
    public static IReadOnlyList<string> Names { get; } = ["far_left", "left", "center", "right", "far_right"];

    public static bool TryParse(string name, out StageSlot slot) => ByName.TryGetValue(name, out slot);

    /// <summary>
    /// Where the middle of a portrait in <paramref name="slot"/> sits, as a fraction of the screen width
    /// (0 is the left edge, 1 the right).
    /// </summary>
    public static float Anchor(StageSlot slot) => slot switch
    {
        StageSlot.FarLeft => 0.14f,
        StageSlot.Left => 0.3f,
        StageSlot.Center => 0.5f,
        StageSlot.Right => 0.7f,
        StageSlot.FarRight => 0.86f,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null),
    };

    /// <summary>
    /// Whether a portrait in <paramref name="slot"/> should be mirrored. Portrait art faces right, so portraits
    /// on the right half face left, towards the middle. <paramref name="flip"/> (the script's <c>flip</c>) reverses that.
    /// </summary>
    public static bool FacesLeft(StageSlot slot, bool flip) => (slot is StageSlot.Right or StageSlot.FarRight) != flip;

    /// <summary>The side a portrait slides in from and out to: -1 for the left edge, 1 for the right.</summary>
    public static int EntrySide(StageSlot slot) => slot <= StageSlot.Center ? -1 : 1;
}

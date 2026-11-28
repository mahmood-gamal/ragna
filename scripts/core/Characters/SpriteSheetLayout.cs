namespace Ragna.Core.Characters;

/// <summary>A pixel rectangle in a sprite sheet: the top-left corner and the size.</summary>
public readonly record struct SheetRegion(int X, int Y, int Width, int Height);

/// <summary>
/// The layout every character sprite sheet follows, so real art can replace the placeholder without
/// code changes. There is one sheet per animation (<c>&lt;character&gt;_&lt;animation&gt;.png</c>, for
/// example <c>novice_walk.png</c>). Each sheet has one row per <see cref="FacingDirection"/>, in enum
/// order (row 0 = south, then clockwise), and one column per frame, left to right in play order. Every
/// frame is <see cref="FrameWidth"/> × <see cref="FrameHeight"/> pixels with no padding, and the
/// character's feet touch the bottom pixel row, centred horizontally (<see cref="AnchorX"/>,
/// <see cref="AnchorY"/>). See docs/characters.md.
/// </summary>
public sealed record SpriteSheetLayout
{
    /// <summary>The layout of the placeholder art: 64 × 96 px frames.</summary>
    public static SpriteSheetLayout Default { get; } = new(64, 96);

    public SpriteSheetLayout(int frameWidth, int frameHeight)
    {
        if (frameWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameWidth), "Must be positive.");
        }

        if (frameHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameHeight), "Must be positive.");
        }

        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
    }

    /// <summary>Width of one frame, in pixels.</summary>
    public int FrameWidth { get; }

    /// <summary>Height of one frame, in pixels.</summary>
    public int FrameHeight { get; }

    /// <summary>Number of rows in a sheet: one per direction.</summary>
    public int Rows => FacingDirections.Count;

    /// <summary>The anchor's column inside a frame: the horizontal centre, where the feet stand.</summary>
    public int AnchorX => FrameWidth / 2;

    /// <summary>The anchor's row inside a frame: the bottom pixel row, where the feet touch the ground.</summary>
    public int AnchorY => FrameHeight - 1;

    /// <summary>The size in pixels of a sheet with <paramref name="frameCount"/> frames per direction.</summary>
    public (int Width, int Height) SheetSize(int frameCount)
    {
        if (frameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameCount), "Must be positive.");
        }

        return (FrameWidth * frameCount, FrameHeight * Rows);
    }

    /// <summary>Where frame <paramref name="frame"/> (0-based) of <paramref name="direction"/> sits in its sheet.</summary>
    public SheetRegion Region(FacingDirection direction, int frame)
    {
        if (frame < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), "Can't be negative.");
        }

        return new SheetRegion(
            frame * FrameWidth, FacingDirections.Row(direction) * FrameHeight, FrameWidth, FrameHeight);
    }

    /// <summary>
    /// The SpriteFrames animation name for <paramref name="animation"/> facing <paramref name="direction"/>,
    /// for example <c>walk_se</c>. <see cref="FacingDirections.ResolveAnimation"/> looks these up.
    /// </summary>
    public static string AnimationName(CharacterAnimation animation, FacingDirection direction) =>
        $"{CharacterAnimator.AnimationName(animation)}_{FacingDirections.Suffix(direction)}";

    /// <summary>The sheet file name for one animation of a character, for example <c>novice_walk.png</c>.</summary>
    public static string SheetFileName(string character, CharacterAnimation animation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(character);
        return $"{character}_{CharacterAnimator.AnimationName(animation)}.png";
    }
}

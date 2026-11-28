using System.Globalization;
using System.Text.RegularExpressions;
using Ragna.Core.Characters;

namespace Ragna.Core.Tests.Characters;

public sealed class SpriteSheetLayoutTests
{
    private static readonly SpriteSheetLayout Layout = SpriteSheetLayout.Default;

    [Fact]
    public void Default_frames_are_64_by_96_with_the_anchor_at_the_feet()
    {
        Assert.Equal(64, Layout.FrameWidth);
        Assert.Equal(96, Layout.FrameHeight);
        Assert.Equal(32, Layout.AnchorX);
        Assert.Equal(95, Layout.AnchorY);
        Assert.Equal(8, Layout.Rows);
    }

    [Theory]
    [InlineData(4, 256, 768)]
    [InlineData(6, 384, 768)]
    public void Sheet_has_a_column_per_frame_and_a_row_per_direction(int frames, int width, int height)
    {
        Assert.Equal((width, height), Layout.SheetSize(frames));
    }

    [Theory]
    [InlineData(FacingDirection.South, 0, 0, 0)]
    [InlineData(FacingDirection.South, 3, 192, 0)]
    [InlineData(FacingDirection.SouthWest, 0, 0, 96)]
    [InlineData(FacingDirection.North, 2, 128, 384)]
    [InlineData(FacingDirection.SouthEast, 5, 320, 672)]
    public void Region_is_column_frame_row_direction(FacingDirection direction, int frame, int x, int y)
    {
        Assert.Equal(new SheetRegion(x, y, 64, 96), Layout.Region(direction, frame));
    }

    [Fact]
    public void Rejects_bad_sizes_and_frames()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpriteSheetLayout(0, 96));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpriteSheetLayout(64, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Layout.SheetSize(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Layout.Region(FacingDirection.South, -1));
    }

    [Theory]
    [InlineData(CharacterAnimation.Idle, FacingDirection.South, "idle_s")]
    [InlineData(CharacterAnimation.Walk, FacingDirection.SouthEast, "walk_se")]
    [InlineData(CharacterAnimation.Attack, FacingDirection.NorthWest, "attack_nw")]
    public void Animation_names_are_base_name_and_direction_suffix(
        CharacterAnimation animation, FacingDirection direction, string expected)
    {
        Assert.Equal(expected, SpriteSheetLayout.AnimationName(animation, direction));
    }

    [Fact]
    public void Sheet_file_names_are_character_and_animation()
    {
        Assert.Equal("novice_walk.png", SpriteSheetLayout.SheetFileName("novice", CharacterAnimation.Walk));
        Assert.Throws<ArgumentException>(() => SpriteSheetLayout.SheetFileName(" ", CharacterAnimation.Idle));
    }

    [Fact]
    public void Animation_names_resolve_to_themselves_when_all_eight_exist()
    {
        var names = new HashSet<string>(
            Enum.GetValues<FacingDirection>().Select(d => SpriteSheetLayout.AnimationName(CharacterAnimation.Walk, d)));

        foreach (var direction in Enum.GetValues<FacingDirection>())
        {
            var (name, flip) = FacingDirections.ResolveAnimation("walk", direction, names.Contains);
            Assert.Equal(SpriteSheetLayout.AnimationName(CharacterAnimation.Walk, direction), name);
            Assert.False(flip);
        }
    }
}

/// <summary>Checks the generated placeholder art in assets/characters/placeholder/ follows the layout.</summary>
public sealed class PlaceholderSpriteSheetTests
{
    private static readonly string Folder = Path.Combine(RepoRoot(), "assets", "characters", "placeholder");

    [Theory]
    [InlineData(CharacterAnimation.Idle, 4)]
    [InlineData(CharacterAnimation.Walk, 6)]
    [InlineData(CharacterAnimation.Attack, 4)]
    public void Sheet_png_has_the_layout_size(CharacterAnimation animation, int frames)
    {
        var path = Path.Combine(Folder, SpriteSheetLayout.SheetFileName("placeholder", animation));
        Assert.Equal(SpriteSheetLayout.Default.SheetSize(frames), PngSize(path));
        Assert.True(File.Exists(path + ".import"), $"{path}.import is missing");
    }

    [Theory]
    [InlineData(CharacterAnimation.Idle, 4)]
    [InlineData(CharacterAnimation.Walk, 6)]
    [InlineData(CharacterAnimation.Attack, 4)]
    public void SpriteFrames_has_every_direction_cut_from_the_right_rows(CharacterAnimation animation, int frames)
    {
        var tres = File.ReadAllText(Path.Combine(Folder, "placeholder_frames.tres"));
        var baseName = CharacterAnimator.AnimationName(animation);

        foreach (var direction in Enum.GetValues<FacingDirection>())
        {
            var name = SpriteSheetLayout.AnimationName(animation, direction);
            Assert.Contains($"\"name\": &\"{name}\"", tres);

            for (var i = 0; i < frames; i++)
            {
                var id = $"AtlasTexture_{baseName}_{FacingDirections.Suffix(direction)}_{i}";
                var match = Regex.Match(
                    tres,
                    $@"id=""{id}""\]\natlas = ExtResource\(""[^""]+_{baseName}""\)\nregion = Rect2\((\d+), (\d+), (\d+), (\d+)\)");
                Assert.True(match.Success, $"{id} is missing");

                var region = new SheetRegion(
                    Int(match.Groups[1]), Int(match.Groups[2]), Int(match.Groups[3]), Int(match.Groups[4]));
                Assert.Equal(SpriteSheetLayout.Default.Region(direction, i), region);
            }
        }

        // The plain name stays as a fallback.
        Assert.Contains($"\"name\": &\"{baseName}\"", tres);
    }

    private static int Int(Group g) => int.Parse(g.Value, CultureInfo.InvariantCulture);

    private static (int Width, int Height) PngSize(string path)
    {
        // Width and height are big-endian ints at bytes 16-23 of the IHDR chunk.
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 24 && bytes[1] == 'P' && bytes[2] == 'N' && bytes[3] == 'G', $"{path} is not a PNG");
        static int BigEndian(byte[] b, int at) => (b[at] << 24) | (b[at + 1] << 16) | (b[at + 2] << 8) | b[at + 3];
        return (BigEndian(bytes, 16), BigEndian(bytes, 20));
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "project.godot")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Can't find the repository root (project.godot).");
    }
}

using System.Buffers.Binary;
using Ragna.Core.Dialogue;

namespace Ragna.Core.Tests.Dialogue;

public sealed class PortraitArtTests
{
    private static string PortraitDirectory => Path.Combine(AppContext.BaseDirectory, "assets", "portraits");

    [Fact]
    public void Builds_the_portrait_path()
    {
        Assert.Equal("assets/portraits/old_man/happy.png", PortraitArt.PathFor("old_man", Expressions.Happy));
    }

    [Theory]
    [InlineData("Elder")]
    [InlineData("old man")]
    [InlineData("../elder")]
    [InlineData("")]
    public void Rejects_bad_character_ids(string character) =>
        Assert.Throws<ArgumentException>(() => PortraitArt.PathFor(character, Expressions.Neutral));

    [Fact]
    public void Rejects_unknown_expressions() =>
        Assert.Throws<ArgumentException>(() => PortraitArt.PathFor("elder", "smug"));

    [Fact]
    public void Every_portrait_follows_the_naming_and_size_rules()
    {
        var characters = Directory.GetDirectories(PortraitDirectory);

        Assert.NotEmpty(characters);
        Assert.All(characters, folder =>
        {
            var character = Path.GetFileName(folder);
            Assert.True(PortraitArt.IsValidCharacterId(character), $"Bad portrait folder name '{character}'.");
            Assert.True(File.Exists(Path.Combine(folder, "neutral.png")), $"{character} has no neutral.png.");

            Assert.All(Directory.GetFiles(folder, "*.png"), file =>
            {
                var expression = Path.GetFileNameWithoutExtension(file);
                Assert.True(Expressions.IsKnown(expression), $"{character}/{expression}.png is not a known expression.");
                Assert.Equal((PortraitArt.Width, PortraitArt.Height), PngSize(file));
            });
        });
    }

    [Fact]
    public void Every_character_in_the_game_dialogue_has_portraits()
    {
        var dialogue = Path.Combine(AppContext.BaseDirectory, "data", "dialogue");
        var characters = Directory.GetFiles(dialogue, "*.json")
            .SelectMany(file => Characters(DialogueScript.FromJson(File.ReadAllText(file)).Commands))
            .Distinct();

        Assert.All(characters, character =>
            Assert.True(File.Exists(Path.Combine(PortraitDirectory, character, "neutral.png")), $"{character} has no portraits."));
    }

    private static IEnumerable<string> Characters(IEnumerable<DialogueCommand> commands) =>
        commands.SelectMany(command => command switch
        {
            EnterCommand enter => [enter.Character],
            ChoiceCommand choice => choice.Options.SelectMany(o => Characters(o.Then)),
            IfFlagCommand branch => Characters(branch.Then).Concat(Characters(branch.Else)),
            _ => [],
        });

    /// <summary>Width and height from a PNG's IHDR chunk.</summary>
    private static (int Width, int Height) PngSize(string file)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = File.OpenRead(file);
        stream.ReadExactly(header);
        Assert.True(header[..8].SequenceEqual(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A }), $"{file} is not a PNG.");
        return (BinaryPrimitives.ReadInt32BigEndian(header[16..20]), BinaryPrimitives.ReadInt32BigEndian(header[20..24]));
    }
}

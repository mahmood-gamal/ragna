using Ragna.Core.Dialogue;

namespace Ragna.Core.Tests.Dialogue;

public sealed class TypewriterTests
{
    [Fact]
    public void Reveals_characters_at_the_set_speed()
    {
        var typewriter = new Typewriter(charactersPerSecond: 10);
        typewriter.Start("Hello there");

        Assert.Equal(0, typewriter.VisibleCharacters);
        typewriter.Update(0.25);
        Assert.Equal(2, typewriter.VisibleCharacters);
        typewriter.Update(0.25);
        Assert.Equal(5, typewriter.VisibleCharacters);
        Assert.False(typewriter.IsComplete);

        typewriter.Update(10);
        Assert.Equal(11, typewriter.VisibleCharacters);
        Assert.True(typewriter.IsComplete);
    }

    [Fact]
    public void Complete_shows_the_whole_line()
    {
        var typewriter = new Typewriter(5);
        typewriter.Start("Slow line");

        typewriter.Complete();

        Assert.True(typewriter.IsComplete);
        Assert.Equal(9, typewriter.VisibleCharacters);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Zero_speed_shows_lines_at_once(double speed)
    {
        var typewriter = new Typewriter(speed);
        typewriter.Start("Instant");

        Assert.True(typewriter.IsComplete);
    }

    [Fact]
    public void Starting_a_new_line_resets_it_and_speed_can_change()
    {
        var typewriter = new Typewriter(100);
        typewriter.Start("First");
        typewriter.Complete();

        typewriter.CharactersPerSecond = 4;
        typewriter.Start("Second");
        typewriter.Update(0.5);

        Assert.Equal("Second", typewriter.Text);
        Assert.Equal(2, typewriter.VisibleCharacters);
    }

    [Fact]
    public void Empty_lines_are_complete_and_negative_time_is_ignored()
    {
        var typewriter = new Typewriter(10);
        typewriter.Start("");
        Assert.True(typewriter.IsComplete);

        typewriter.Start("abc");
        typewriter.Update(-1);
        Assert.Equal(0, typewriter.VisibleCharacters);
    }
}

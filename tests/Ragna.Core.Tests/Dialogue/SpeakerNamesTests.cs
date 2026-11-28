using Ragna.Core.Dialogue;

namespace Ragna.Core.Tests.Dialogue;

public sealed class SpeakerNamesTests
{
    [Theory]
    [InlineData("elder", "Elder")]
    [InlineData("old_man", "Old Man")]
    [InlineData("guard-captain", "Guard Captain")]
    [InlineData("Hero", "Hero")]
    [InlineData("a__b", "A B")]
    public void Title_cases_ids(string id, string expected)
    {
        Assert.Equal(expected, SpeakerNames.Display(id));
    }
}

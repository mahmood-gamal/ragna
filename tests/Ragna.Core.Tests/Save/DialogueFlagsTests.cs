using Ragna.Core.Save;

namespace Ragna.Core.Tests.Save;

public sealed class DialogueFlagsTests
{
    [Fact]
    public void Unset_flags_read_as_false()
    {
        var flags = new DialogueFlags();

        Assert.False(flags.Get("met_elder"));
        Assert.False(flags.Has("met_elder"));
    }

    [Fact]
    public void Set_raises_changed_only_when_the_value_changes()
    {
        var flags = new DialogueFlags();
        var changes = new List<(string, bool)>();
        flags.Changed += (name, value) => changes.Add((name, value));

        flags.Set("met_elder");
        flags.Set("met_elder");
        flags.Set("met_elder", false);

        Assert.Equal([("met_elder", true), ("met_elder", false)], changes);
    }

    [Fact]
    public void Names_are_case_sensitive()
    {
        var flags = new DialogueFlags();

        flags.Set("Met_Elder");

        Assert.False(flags.Get("met_elder"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_blank_names(string name)
    {
        Assert.Throws<ArgumentException>(() => new DialogueFlags().Set(name));
    }

    [Fact]
    public void Snapshot_is_a_sorted_copy()
    {
        var flags = new DialogueFlags();
        flags.Set("b");
        flags.Set("a");

        var snapshot = flags.Snapshot();
        flags.Set("c");

        Assert.Equal(["a", "b"], snapshot.Keys);
    }
}

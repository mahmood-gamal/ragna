using Ragna.Core.Dialogue;
using Ragna.Core.Save;

namespace Ragna.Core.Tests.Dialogue;

public sealed class StoryTriggerRuleTests
{
    [Fact]
    public void A_play_once_trigger_plays_until_it_is_marked()
    {
        var flags = new DialogueFlags();
        var rule = new StoryTriggerRule("village_arrival");

        Assert.True(rule.ShouldPlay(flags));
        rule.MarkPlayed(flags);

        Assert.False(rule.ShouldPlay(flags));
        Assert.True(flags.Get("trigger.village_arrival"));
    }

    [Fact]
    public void Uses_the_given_once_flag()
    {
        var flags = new DialogueFlags();
        var rule = new StoryTriggerRule("village_arrival", onceFlag: "saw_village");

        rule.MarkPlayed(flags);

        Assert.Equal("saw_village", rule.OnceFlag);
        Assert.True(flags.Get("saw_village"));
        Assert.False(flags.Has("trigger.village_arrival"));
    }

    [Fact]
    public void A_repeating_trigger_always_plays_and_sets_no_flag()
    {
        var flags = new DialogueFlags();
        var rule = new StoryTriggerRule("village_arrival", once: false);

        rule.MarkPlayed(flags);

        Assert.True(rule.ShouldPlay(flags));
        Assert.Empty(flags.All);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public void Waits_for_the_required_flag(bool requiredValue, bool flagValue, bool plays)
    {
        var flags = new DialogueFlags();
        flags.Set("met_elder", flagValue);
        var rule = new StoryTriggerRule("village_arrival", requiredFlag: "met_elder", requiredValue: requiredValue);

        Assert.Equal(plays, rule.ShouldPlay(flags));
    }

    [Fact]
    public void A_required_flag_never_set_reads_as_false()
    {
        var rule = new StoryTriggerRule("village_arrival", requiredFlag: "met_elder");

        Assert.False(rule.ShouldPlay(new DialogueFlags()));
    }

    [Fact]
    public void A_played_trigger_stays_played_after_a_save_and_load()
    {
        var flags = new DialogueFlags();
        var rule = new StoryTriggerRule("village_arrival");
        rule.MarkPlayed(flags);

        var json = SaveSerializer.ToJson(new SaveData { DialogueFlags = flags.Snapshot() });
        var loaded = new DialogueFlags();
        loaded.Load(SaveSerializer.FromJson(json).DialogueFlags);

        Assert.False(rule.ShouldPlay(loaded));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Needs_a_dialogue_id(string id) =>
        Assert.ThrowsAny<ArgumentException>(() => new StoryTriggerRule(id));
}

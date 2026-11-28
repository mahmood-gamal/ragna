using Ragna.Core.Dialogue;
using Ragna.Core.Save;

namespace Ragna.Core.Tests.Dialogue;

/// <summary>The sample NPC conversation, <c>data/dialogue/elder_greeting.json</c>.</summary>
public sealed class SampleConversationTests
{
    private const string PromisedHelp = "promised_help";

    private static DialogueScript Script() =>
        DialogueScript.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "dialogue", "elder_greeting.json")));

    /// <summary>Plays the scene, picking <paramref name="choices"/> in order, and returns every event.</summary>
    private static List<DialogueEvent> Play(DialogueFlags flags, params int[] choices)
    {
        var runner = new DialogueRunner(Script(), flags);
        var events = new List<DialogueEvent>();
        var picks = new Queue<int>(choices);
        while (true)
        {
            var next = runner.Advance();
            events.Add(next);
            switch (next)
            {
                case DialogueEnded:
                    return events;
                case ChoiceShown:
                    runner.Choose(picks.Dequeue());
                    break;
            }
        }
    }

    private static DialogueFlags SaveAndLoad(DialogueFlags flags)
    {
        var json = SaveSerializer.ToJson(new SaveData { DialogueFlags = flags.Snapshot() });
        var loaded = new DialogueFlags();
        loaded.Load(SaveSerializer.FromJson(json).DialogueFlags);
        return loaded;
    }

    [Fact]
    public void Has_two_characters_who_change_expression()
    {
        var events = Play(new DialogueFlags(), 0);

        Assert.Equal(["elder", "hero"], events.OfType<CharacterEntered>().Select(e => e.Character).Order());
        var changes = events.OfType<ExpressionChanged>().ToList();
        Assert.Contains(changes, e => e.Character == "elder");
        Assert.Contains(changes, e => e.Character == "hero");
        Assert.Equal(["elder", "hero"], events.OfType<LineShown>().Select(l => l.Speaker).OfType<string>().Distinct().Order());
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void The_choice_sets_the_flag(int choice, bool promised)
    {
        var flags = new DialogueFlags();

        var events = Play(flags, choice);

        Assert.Single(events.OfType<ChoiceShown>());
        Assert.True(flags.Has(PromisedHelp));
        Assert.Equal(promised, flags.Get(PromisedHelp));
    }

    [Fact]
    public void A_promise_survives_a_save_and_load_and_changes_the_next_talk()
    {
        var flags = new DialogueFlags();
        Play(flags, 0);

        var loaded = SaveAndLoad(flags);
        var again = Play(loaded);

        Assert.True(loaded.Get(PromisedHelp));
        Assert.Empty(again.OfType<ChoiceShown>());
        Assert.Contains(again.OfType<LineShown>(), l => l.Text.StartsWith("Thank you again", StringComparison.Ordinal));
    }

    [Fact]
    public void After_refusing_the_elder_asks_again_without_the_introduction()
    {
        var flags = new DialogueFlags();
        var first = Play(flags, 1);

        var again = Play(SaveAndLoad(flags), 0);

        Assert.Contains(first.OfType<LineShown>(), l => l.Text.StartsWith("Ah, a traveller", StringComparison.Ordinal));
        Assert.DoesNotContain(again.OfType<LineShown>(), l => l.Text.StartsWith("Ah, a traveller", StringComparison.Ordinal));
        Assert.Contains(again.OfType<LineShown>(), l => l.Text.StartsWith("Back again", StringComparison.Ordinal));
        Assert.Single(again.OfType<ChoiceShown>());
    }
}

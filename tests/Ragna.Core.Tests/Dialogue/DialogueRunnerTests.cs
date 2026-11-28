using Ragna.Core.Dialogue;
using Ragna.Core.Save;

namespace Ragna.Core.Tests.Dialogue;

public sealed class DialogueRunnerTests
{
    private static DialogueRunner Runner(string commands, DialogueFlags? flags = null) =>
        new(DialogueScript.FromJson($$"""{ "id": "test", "commands": [ {{commands}} ] }"""), flags ?? new DialogueFlags());

    /// <summary>Advances until the script ends or waits for a choice, collecting every event.</summary>
    private static List<DialogueEvent> RunToChoiceOrEnd(DialogueRunner runner)
    {
        var events = new List<DialogueEvent>();
        while (true)
        {
            var next = runner.Advance();
            events.Add(next);
            if (next is DialogueEnded or ChoiceShown)
            {
                return events;
            }
        }
    }

    private static List<string> Lines(IEnumerable<DialogueEvent> events) =>
        events.OfType<LineShown>().Select(l => l.Text).ToList();

    [Fact]
    public void Plays_commands_in_order_then_ends()
    {
        var runner = Runner("""
            { "cmd": "background", "image": "village" },
            { "cmd": "enter", "character": "elder", "slot": "right" },
            { "cmd": "say", "speaker": "elder", "text": "Hello." },
            { "cmd": "wait", "seconds": 2 },
            { "cmd": "expression", "character": "elder", "expression": "happy" },
            { "cmd": "exit", "character": "elder" }
            """);

        Assert.Equal<DialogueEvent>(
            [
                new BackgroundChanged("village"),
                new CharacterEntered("elder", StageSlot.Right, "neutral"),
                new LineShown("elder", "Hello."),
                new WaitRequested(2),
                new ExpressionChanged("elder", StageSlot.Right, "happy"),
                new CharacterExited("elder", StageSlot.Right),
                new DialogueEnded(),
            ],
            RunToChoiceOrEnd(runner));
        Assert.True(runner.Finished);
        Assert.IsType<DialogueEnded>(runner.Advance());
    }

    [Fact]
    public void Instant_is_false_only_for_events_the_ui_waits_on()
    {
        Assert.True(new CharacterEntered("a", StageSlot.Left, "n").Instant);
        Assert.True(new BackgroundChanged(null).Instant);
        Assert.False(new LineShown(null, "t").Instant);
        Assert.False(new WaitRequested(1).Instant);
        Assert.False(new ChoiceShown(["a"]).Instant);
        Assert.False(new DialogueEnded().Instant);
    }

    [Fact]
    public void Empty_script_ends_at_once()
    {
        var runner = new DialogueRunner(new DialogueScript("empty", []), new DialogueFlags());

        Assert.IsType<DialogueEnded>(runner.Advance());
        Assert.True(runner.Finished);
    }

    [Theory]
    [InlineData(0, "Yes!")]
    [InlineData(1, "No...")]
    public void Choice_runs_the_picked_branch_then_continues_after_the_choice(int pick, string expected)
    {
        var runner = Runner("""
            { "cmd": "choice", "options": [
              { "text": "Yes", "then": [ { "cmd": "say", "text": "Yes!" } ] },
              { "text": "No", "then": [ { "cmd": "say", "text": "No..." } ] }
            ] },
            { "cmd": "say", "text": "After." }
            """);

        Assert.Equal(new ChoiceShown(["Yes", "No"]).Options, Assert.IsType<ChoiceShown>(runner.Advance()).Options);
        Assert.True(runner.AwaitingChoice);
        runner.Choose(pick);
        Assert.False(runner.AwaitingChoice);

        Assert.Equal([expected, "After."], Lines(RunToChoiceOrEnd(runner)));
    }

    [Fact]
    public void Advance_during_a_choice_throws_and_bad_indexes_are_rejected()
    {
        var runner = Runner("""{ "cmd": "choice", "options": [ { "text": "Only" } ] }""");

        Assert.Throws<InvalidOperationException>(() => runner.Choose(0));
        runner.Advance();
        Assert.Throws<InvalidOperationException>(() => runner.Advance());
        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Choose(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Choose(-1));

        runner.Choose(0);
        Assert.IsType<DialogueEnded>(runner.Advance());
    }

    [Fact]
    public void Options_with_if_flag_show_only_when_the_flag_matches_and_indexes_count_shown_options()
    {
        var flags = new DialogueFlags();
        flags.Set("has_key");
        var runner = Runner("""
            { "cmd": "choice", "options": [
              { "text": "Pick the lock", "ifFlag": "is_thief" },
              { "text": "Use the key", "ifFlag": "has_key", "then": [ { "cmd": "say", "text": "Click." } ] },
              { "text": "Knock", "ifFlag": "has_key", "ifValue": false },
              { "text": "Leave", "then": [ { "cmd": "say", "text": "Bye." } ] }
            ] }
            """, flags);

        Assert.Equal(["Use the key", "Leave"], Assert.IsType<ChoiceShown>(runner.Advance()).Options);
        runner.Choose(1);

        Assert.Equal(["Bye."], Lines(RunToChoiceOrEnd(runner)));
    }

    [Fact]
    public void Choice_with_no_shown_options_is_skipped()
    {
        var runner = Runner("""
            { "cmd": "choice", "options": [ { "text": "Secret", "ifFlag": "nope" } ] },
            { "cmd": "say", "text": "Next." }
            """);

        Assert.Equal(new LineShown(null, "Next."), runner.Advance());
    }

    [Theory]
    [InlineData(true, "Welcome back.")]
    [InlineData(false, "Who are you?")]
    public void If_flag_picks_then_or_else(bool met, string expected)
    {
        var flags = new DialogueFlags();
        flags.Set("met", met);
        var runner = Runner("""
            { "cmd": "if_flag", "flag": "met",
              "then": [ { "cmd": "say", "text": "Welcome back." } ],
              "else": [ { "cmd": "say", "text": "Who are you?" } ] },
            { "cmd": "say", "text": "Anyway." }
            """, flags);

        Assert.Equal([expected, "Anyway."], Lines(RunToChoiceOrEnd(runner)));
    }

    [Fact]
    public void If_flag_with_value_false_tests_for_an_unset_flag()
    {
        var runner = Runner("""
            { "cmd": "if_flag", "flag": "met", "value": false, "then": [ { "cmd": "say", "text": "First time." } ] }
            """);

        Assert.Equal(["First time."], Lines(RunToChoiceOrEnd(runner)));
    }

    [Fact]
    public void Set_flag_writes_to_the_shared_flags_and_later_checks_see_it()
    {
        var flags = new DialogueFlags();
        var runner = Runner("""
            { "cmd": "set_flag", "flag": "met" },
            { "cmd": "set_flag", "flag": "angry", "value": false },
            { "cmd": "if_flag", "flag": "met", "then": [ { "cmd": "say", "text": "Seen." } ] }
            """, flags);

        Assert.Equal(["Seen."], Lines(RunToChoiceOrEnd(runner)));
        Assert.True(flags.Get("met"));
        Assert.True(flags.Has("angry"));
        Assert.False(flags.Get("angry"));
    }

    [Fact]
    public void Flags_carry_over_between_runs_of_the_same_script()
    {
        var flags = new DialogueFlags();
        var script = DialogueScript.FromJson("""
            { "id": "elder", "commands": [
              { "cmd": "if_flag", "flag": "met",
                "then": [ { "cmd": "say", "text": "Again?" } ],
                "else": [ { "cmd": "say", "text": "Hello." }, { "cmd": "set_flag", "flag": "met" } ] }
            ] }
            """);

        Assert.Equal(["Hello."], Lines(RunToChoiceOrEnd(new DialogueRunner(script, flags))));
        Assert.Equal(["Again?"], Lines(RunToChoiceOrEnd(new DialogueRunner(script, flags))));
    }

    [Fact]
    public void Nested_branches_unwind_back_to_the_outer_block()
    {
        var flags = new DialogueFlags();
        flags.Set("a");
        var runner = Runner("""
            { "cmd": "choice", "options": [ { "text": "Go", "then": [
              { "cmd": "if_flag", "flag": "a", "then": [
                { "cmd": "say", "text": "inner" },
                { "cmd": "choice", "options": [ { "text": "Deeper", "then": [ { "cmd": "say", "text": "deepest" } ] } ] },
                { "cmd": "say", "text": "after inner choice" }
              ] },
              { "cmd": "say", "text": "after if" }
            ] } ] },
            { "cmd": "say", "text": "after outer choice" }
            """, flags);

        runner.Advance();
        runner.Choose(0);
        var first = RunToChoiceOrEnd(runner);
        Assert.Equal(["inner"], Lines(first));
        Assert.IsType<ChoiceShown>(first[^1]);
        runner.Choose(0);

        Assert.Equal(["deepest", "after inner choice", "after if", "after outer choice"], Lines(RunToChoiceOrEnd(runner)));
    }

    [Fact]
    public void End_stops_the_script_from_inside_a_branch()
    {
        var runner = Runner("""
            { "cmd": "if_flag", "flag": "x", "value": false, "then": [ { "cmd": "say", "text": "Bye." }, { "cmd": "end" } ] },
            { "cmd": "say", "text": "Never shown." }
            """);

        var events = RunToChoiceOrEnd(runner);

        Assert.Equal(["Bye."], Lines(events));
        Assert.True(runner.Finished);
        Assert.IsType<DialogueEnded>(runner.Advance());
    }

    [Fact]
    public void Stage_tracks_who_stands_where_and_the_background()
    {
        var runner = Runner("""
            { "cmd": "background", "image": "forest" },
            { "cmd": "enter", "character": "elder", "slot": "left" },
            { "cmd": "enter", "character": "hero", "slot": "right", "expression": "angry" },
            { "cmd": "expression", "character": "elder", "expression": "happy" },
            { "cmd": "say", "text": "Pause." },
            { "cmd": "exit" },
            { "cmd": "background" }
            """);

        Assert.IsType<LineShown>(RunUntilLine(runner));
        Assert.Equal("forest", runner.Background);
        Assert.Equal(new StageActor("elder", "happy"), runner.Stage[StageSlot.Left]);
        Assert.Equal(new StageActor("hero", "angry"), runner.Stage[StageSlot.Right]);

        var rest = RunToChoiceOrEnd(runner);
        Assert.Equal<DialogueEvent>(
            [
                new CharacterExited("elder", StageSlot.Left),
                new CharacterExited("hero", StageSlot.Right),
                new BackgroundChanged(null),
                new DialogueEnded(),
            ],
            rest);
        Assert.Empty(runner.Stage);
        Assert.Null(runner.Background);
    }

    [Fact]
    public void Entering_again_moves_the_character_and_replaces_whoever_stood_there()
    {
        var runner = Runner("""
            { "cmd": "enter", "character": "elder", "slot": "left" },
            { "cmd": "enter", "character": "hero", "slot": "right" },
            { "cmd": "enter", "character": "elder", "slot": "right", "expression": "sad" }
            """);

        var events = RunToChoiceOrEnd(runner);

        Assert.Equal<DialogueEvent>(
            [
                new CharacterEntered("elder", StageSlot.Left, "neutral"),
                new CharacterEntered("hero", StageSlot.Right, "neutral"),
                new CharacterExited("elder", StageSlot.Left),
                new CharacterExited("hero", StageSlot.Right),
                new CharacterEntered("elder", StageSlot.Right, "sad"),
                new DialogueEnded(),
            ],
            events);
        Assert.Equal(new StageActor("elder", "sad"), Assert.Single(runner.Stage).Value);
    }

    [Theory]
    [InlineData("""{ "cmd": "expression", "character": "ghost", "expression": "sad" }""")]
    [InlineData("""{ "cmd": "exit", "character": "ghost" }""")]
    public void Touching_a_character_who_is_not_on_stage_names_the_script(string command)
    {
        var runner = Runner(command);

        var error = Assert.Throws<InvalidDataException>(() => runner.Advance());

        Assert.Contains("'test'", error.Message);
        Assert.Contains("'ghost'", error.Message);
    }

    [Fact]
    public void Example_script_plays_both_first_and_repeat_visits()
    {
        var flags = new DialogueFlags();
        var script = DialogueScript.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "dialogue", "example.json")));

        var first = new DialogueRunner(script, flags);
        var choice = Assert.IsType<ChoiceShown>(RunToChoiceOrEnd(first)[^1]);
        Assert.Equal(["Any work for me?", "Goodbye."], choice.Options);
        first.Choose(0);
        RunToChoiceOrEnd(first);
        Assert.True(flags.Get("met_elder"));
        Assert.True(flags.Get("slime_quest"));
        Assert.Empty(first.Stage);

        var second = new DialogueRunner(script, flags);
        var events = RunToChoiceOrEnd(second);
        Assert.Contains("Back again, traveller?", Lines(events));
        Assert.Equal(3, Assert.IsType<ChoiceShown>(events[^1]).Options.Count);
        second.Choose(2);
        Assert.Equal(["I'll be on my way."], Lines(RunToChoiceOrEnd(second)));
    }

    private static DialogueEvent RunUntilLine(DialogueRunner runner)
    {
        DialogueEvent next;
        do
        {
            next = runner.Advance();
        }
        while (next.Instant);

        return next;
    }

    [Fact]
    public void Entering_keeps_flip_on_the_event_and_the_stage()
    {
        var runner = Runner("""{ "cmd": "enter", "character": "elder", "slot": "far_right", "flip": true }""");

        Assert.Equal(new CharacterEntered("elder", StageSlot.FarRight, "neutral", Flip: true), runner.Advance());
        Assert.Equal(new StageActor("elder", "neutral", Flip: true), runner.Stage[StageSlot.FarRight]);
    }

    [Fact]
    public void Everyone_exits_left_to_right_in_a_group_of_four()
    {
        var runner = Runner("""
            { "cmd": "enter", "character": "d", "slot": "far_right" },
            { "cmd": "enter", "character": "b", "slot": "left" },
            { "cmd": "enter", "character": "a", "slot": "far_left" },
            { "cmd": "enter", "character": "c", "slot": "right" },
            { "cmd": "exit" }
            """);

        var exits = RunToChoiceOrEnd(runner).OfType<CharacterExited>().Select(e => e.Character);

        Assert.Equal(["a", "b", "c", "d"], exits);
    }
}

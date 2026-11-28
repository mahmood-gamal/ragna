using Ragna.Core.Dialogue;
using Ragna.Core.Save;

namespace Ragna.Core.Tests.Dialogue;

public sealed class DialogueSessionTests
{
    private static DialogueSession Session(string commands, double speed = 10, DialogueFlags? flags = null) =>
        new(new DialogueRunner(
            DialogueScript.FromJson($$"""{ "id": "test", "commands": [ {{commands}} ] }"""),
            flags ?? new DialogueFlags()), speed);

    [Fact]
    public void Applies_stage_changes_then_types_the_first_line()
    {
        var session = Session("""
            { "cmd": "background", "image": "village" },
            { "cmd": "enter", "character": "elder", "slot": "right" },
            { "cmd": "say", "speaker": "elder", "text": "Hello." }
            """);
        var stage = new List<DialogueEvent>();
        var lines = 0;
        session.StageChanged += stage.Add;
        session.LineStarted += () => lines++;

        session.Start();

        Assert.Equal<DialogueEvent>(
            [new BackgroundChanged("village"), new CharacterEntered("elder", StageSlot.Right, "neutral")],
            stage);
        Assert.Equal(1, lines);
        Assert.Equal(DialogueSessionState.Typing, session.State);
        Assert.Equal("elder", session.Speaker);
        Assert.Equal("Hello.", session.Text);
        Assert.Equal(0, session.Typewriter.VisibleCharacters);
    }

    [Fact]
    public void Confirm_completes_a_typing_line_then_advances()
    {
        var session = Session("""
            { "cmd": "say", "text": "One." },
            { "cmd": "say", "speaker": "hero", "text": "Two." }
            """);
        session.Start();

        session.Confirm();
        Assert.Equal(DialogueSessionState.LineComplete, session.State);
        Assert.Equal("One.", session.Text);
        Assert.True(session.Typewriter.IsComplete);

        session.Confirm();
        Assert.Equal(DialogueSessionState.Typing, session.State);
        Assert.Equal("Two.", session.Text);
        Assert.Equal("hero", session.Speaker);
    }

    [Fact]
    public void A_line_finishes_typing_by_itself_and_then_waits_for_confirm()
    {
        var session = Session("""{ "cmd": "say", "text": "Hi." }""", speed: 10);
        session.Start();

        session.Update(0.2);
        Assert.Equal(DialogueSessionState.Typing, session.State);
        session.Update(0.2);
        Assert.Equal(DialogueSessionState.LineComplete, session.State);

        session.Update(5);
        Assert.Equal(DialogueSessionState.LineComplete, session.State);
    }

    [Fact]
    public void Waits_count_down_and_confirm_does_not_cut_them_short()
    {
        var session = Session("""
            { "cmd": "wait", "seconds": 1 },
            { "cmd": "say", "text": "After." }
            """);
        session.Start();
        Assert.Equal(DialogueSessionState.Waiting, session.State);

        session.Confirm();
        session.Update(0.6);
        Assert.Equal(DialogueSessionState.Waiting, session.State);

        session.Update(0.6);
        Assert.Equal(DialogueSessionState.Typing, session.State);
        Assert.Equal("After.", session.Text);
    }

    [Fact]
    public void Skip_runs_through_lines_and_waits_but_stops_at_choices()
    {
        var session = Session("""
            { "cmd": "say", "text": "One." },
            { "cmd": "wait", "seconds": 30 },
            { "cmd": "say", "text": "Two." },
            { "cmd": "choice", "options": [ { "text": "A" }, { "text": "B" } ] },
            { "cmd": "say", "text": "Three." }
            """);
        session.Start();

        for (var i = 0; i < 10; i++)
        {
            session.Update(0.016, skip: true);
        }

        Assert.Equal(DialogueSessionState.Choosing, session.State);
        Assert.Equal(["A", "B"], session.Options);
        Assert.Equal(["One.", "Two."], session.History.Entries.Select(e => e.Text));
    }

    [Fact]
    public void Choosing_sets_flags_follows_the_branch_and_logs_the_pick()
    {
        var flags = new DialogueFlags();
        var session = Session(
            """
            { "cmd": "choice", "options": [
              { "text": "Help", "then": [ { "cmd": "set_flag", "flag": "helped" }, { "cmd": "say", "text": "Thanks!" } ] },
              { "text": "Leave", "then": [ { "cmd": "end" } ] }
            ] },
            { "cmd": "say", "text": "Done." }
            """,
            flags: flags);
        var shown = 0;
        session.ChoiceShown += () => shown++;
        session.Start();
        Assert.Equal(1, shown);

        session.Choose(0);

        Assert.True(flags.Get("helped"));
        Assert.Empty(session.Options);
        Assert.Equal("Thanks!", session.Text);
        Assert.Equal(
            [new HistoryEntry(null, "Help", IsChoice: true), new HistoryEntry(null, "Thanks!", IsChoice: false)],
            session.History.Entries);
    }

    [Fact]
    public void Choose_rejects_bad_calls()
    {
        var session = Session("""
            { "cmd": "say", "text": "Hi." },
            { "cmd": "choice", "options": [ { "text": "A" }, { "text": "B" } ] }
            """);
        session.Start();
        Assert.Throws<InvalidOperationException>(() => session.Choose(0));

        session.Confirm();
        session.Confirm();
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Choose(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Choose(-1));
        Assert.Equal(DialogueSessionState.Choosing, session.State);
    }

    [Fact]
    public void Ends_once_and_ignores_input_afterwards()
    {
        var session = Session("""
            { "cmd": "enter", "character": "elder", "slot": "left" },
            { "cmd": "say", "text": "Bye." },
            { "cmd": "exit" }
            """);
        var ended = 0;
        var exits = new List<DialogueEvent>();
        session.Ended += () => ended++;
        session.StageChanged += e => { if (e is CharacterExited) exits.Add(e); };
        session.Start();

        session.Confirm();
        session.Confirm();
        session.Confirm();
        session.Update(1, skip: true);

        Assert.Equal(DialogueSessionState.Ended, session.State);
        Assert.Equal(1, ended);
        Assert.Equal<DialogueEvent>([new CharacterExited("elder", StageSlot.Left)], exits);
    }

    [Fact]
    public void Start_twice_throws()
    {
        var session = Session("""{ "cmd": "say", "text": "Hi." }""");
        session.Start();

        Assert.Throws<InvalidOperationException>(session.Start);
    }

    [Fact]
    public void History_raises_added_for_each_entry()
    {
        var history = new DialogueHistory();
        var added = new List<HistoryEntry>();
        history.Added += added.Add;

        history.AddLine("elder", "Hi.");
        history.AddLine(null, "Wind.");
        history.AddChoice("Leave");

        Assert.Equal(history.Entries, added);
        Assert.Equal(3, history.Entries.Count);
    }
}

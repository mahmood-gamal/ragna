using Ragna.Core.Dialogue;

namespace Ragna.Core.Tests.Dialogue;

public sealed class DialogueScriptTests
{
    private static string DialogueDirectory => Path.Combine(AppContext.BaseDirectory, "data", "dialogue");

    [Fact]
    public void Every_game_dialogue_file_loads_with_an_id_matching_its_file_name()
    {
        var files = Directory.GetFiles(DialogueDirectory, "*.json");

        Assert.NotEmpty(files);
        Assert.All(files, file =>
        {
            var script = DialogueScript.FromJson(File.ReadAllText(file));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), script.Id);
            Assert.NotEmpty(script.Commands);
        });
    }

    [Fact]
    public void Reads_every_command()
    {
        var script = DialogueScript.FromJson("""
            {
              "id": "all",
              "commands": [
                { "cmd": "enter", "character": "elder", "slot": "left", "expression": "sad" },
                { "cmd": "enter", "character": "hero", "slot": "center" },
                { "cmd": "expression", "character": "elder", "expression": "smile" },
                { "cmd": "say", "speaker": "elder", "text": "Hi." },
                { "cmd": "say", "text": "Wind blows." },
                { "cmd": "choice", "options": [ { "text": "A", "then": [ { "cmd": "end" } ] }, { "text": "B", "ifFlag": "f", "ifValue": false } ] },
                { "cmd": "set_flag", "flag": "f" },
                { "cmd": "set_flag", "flag": "g", "value": false },
                { "cmd": "if_flag", "flag": "f", "then": [ { "cmd": "wait", "seconds": 1.5 } ] },
                { "cmd": "background", "image": "bg/village" },
                { "cmd": "background" },
                { "cmd": "exit", "character": "hero" },
                { "cmd": "exit" },
              ]
            }
            """);

        Assert.Equal("all", script.Id);
        Assert.Equal<DialogueCommand>(
            [
                new EnterCommand("elder", StageSlot.Left, "sad"),
                new EnterCommand("hero", StageSlot.Center, DialogueRunner.DefaultExpression),
                new ExpressionCommand("elder", "smile"),
                new SayCommand("elder", "Hi."),
                new SayCommand(null, "Wind blows."),
            ],
            script.Commands.Take(5));

        var choice = Assert.IsType<ChoiceCommand>(script.Commands[5]);
        Assert.Equal(["A", "B"], choice.Options.Select(o => o.Text));
        Assert.IsType<EndCommand>(Assert.Single(choice.Options[0].Then));
        Assert.Empty(choice.Options[1].Then);
        Assert.Equal("f", choice.Options[1].IfFlag);
        Assert.False(choice.Options[1].IfValue);

        Assert.Equal(new SetFlagCommand("f", true), script.Commands[6]);
        Assert.Equal(new SetFlagCommand("g", false), script.Commands[7]);
        var branch = Assert.IsType<IfFlagCommand>(script.Commands[8]);
        Assert.True(branch.Value);
        Assert.Equal(new WaitCommand(1.5), Assert.Single(branch.Then));
        Assert.Empty(branch.Else);
        Assert.Equal(new BackgroundCommand("bg/village"), script.Commands[9]);
        Assert.Equal(new BackgroundCommand(null), script.Commands[10]);
        Assert.Equal(new ExitCommand("hero"), script.Commands[11]);
        Assert.Equal(new ExitCommand(null), script.Commands[12]);
    }

    [Theory]
    [InlineData("""{ "id": "x" }""", "needs a \"commands\" array")]
    [InlineData("""{ "commands": [] }""", "needs a \"id\"")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "dance" } ] }""", "commands[0] has unknown cmd \"dance\"")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "say" } ] }""", "commands[0] needs a \"text\"")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "say", "text": "a", "speakr": "b" } ] }""", "unknown field \"speakr\"")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "enter", "character": "a", "slot": "top" } ] }""", "unknown slot \"top\"")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "wait", "seconds": -1 } ] }""", "must be 0 or more")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "wait" } ] }""", "needs a number \"seconds\"")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "choice", "options": [] } ] }""", "needs at least one option")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "set_flag", "flag": "a", "value": "yes" } ] }""", "not true or false")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "say", "text": 3 } ] }""", "not a string")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "say", "text": " " } ] }""", "empty \"text\"")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "if_flag", "flag": "a", "then": [ { "cmd": "end" }, { "cmd": "nope" } ] } ] }""", "commands[0].then[1] has unknown cmd")]
    [InlineData("""{ "id": "x", "commands": [ { "cmd": "choice", "options": [ { "text": "a", "then": [ { "cmd": "say" } ] } ] } ] }""", "commands[0].options[0].then[0] needs a \"text\"")]
    [InlineData("""[]""", "must be an object")]
    [InlineData("""{ "id": "x", "commands": [ """, "Invalid dialogue JSON")]
    public void Rejects_bad_scripts_and_says_where(string json, string expected)
    {
        var error = Assert.Throws<InvalidDataException>(() => DialogueScript.FromJson(json));

        Assert.Contains(expected, error.Message);
    }
}

using System.Text.Json;

namespace Ragna.Core.Dialogue;

/// <summary>
/// A dialogue scene read from JSON. A new scene is a new file in <c>data/dialogue/</c>; it needs no code.
/// Run it with <see cref="DialogueRunner"/>. The format is described in <c>docs/dialogue.md</c>.
/// </summary>
public sealed class DialogueScript
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public DialogueScript(string id, IReadOnlyList<DialogueCommand> commands)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(commands);
        Id = id;
        Commands = commands;
    }

    public string Id { get; }

    public IReadOnlyList<DialogueCommand> Commands { get; }

    /// <summary>
    /// Parses and validates <paramref name="json"/>:
    /// <c>{ "id": "elder_intro", "commands": [ { "cmd": "say", "speaker": "elder", "text": "Hello." } ] }</c>.
    /// </summary>
    /// <exception cref="InvalidDataException">
    /// The JSON is malformed, a command is unknown, or a field is missing, misspelled or has the wrong type.
    /// The message names the place, for example <c>commands[2].options[0].then[1]</c>.
    /// </exception>
    public static DialogueScript FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, DocumentOptions);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Invalid dialogue JSON: {e.Message}", e);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Dialogue JSON must be an object with \"id\" and \"commands\".");
            }

            CheckKeys(root, "script", "id", "commands");
            var id = RequiredString(root, "id", "script");
            var commands = ReadBlock(root, "commands", "commands", required: true);
            return new DialogueScript(id, commands);
        }
    }

    private static List<DialogueCommand> ReadBlock(JsonElement parent, string key, string path, bool required)
    {
        if (!TryGet(parent, key, out var array))
        {
            return required ? throw Error(path, $"needs a \"{key}\" array") : [];
        }

        if (array.ValueKind != JsonValueKind.Array)
        {
            throw Error(path, "must be an array of commands");
        }

        var commands = new List<DialogueCommand>();
        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            commands.Add(ReadCommand(item, $"{path}[{index}]"));
            index++;
        }

        return commands;
    }

    private static DialogueCommand ReadCommand(JsonElement e, string path)
    {
        if (e.ValueKind != JsonValueKind.Object)
        {
            throw Error(path, "must be an object with a \"cmd\"");
        }

        var cmd = RequiredString(e, "cmd", path);
        switch (cmd)
        {
            case "enter":
                CheckKeys(e, path, "cmd", "character", "slot", "expression");
                return new EnterCommand(
                    RequiredString(e, "character", path),
                    ReadSlot(e, path),
                    OptionalString(e, "expression", path) ?? DialogueRunner.DefaultExpression);
            case "exit":
                CheckKeys(e, path, "cmd", "character");
                return new ExitCommand(OptionalString(e, "character", path));
            case "expression":
                CheckKeys(e, path, "cmd", "character", "expression");
                return new ExpressionCommand(RequiredString(e, "character", path), RequiredString(e, "expression", path));
            case "say":
                CheckKeys(e, path, "cmd", "speaker", "text");
                return new SayCommand(OptionalString(e, "speaker", path), RequiredString(e, "text", path));
            case "choice":
                CheckKeys(e, path, "cmd", "options");
                return new ChoiceCommand(ReadOptions(e, path));
            case "set_flag":
                CheckKeys(e, path, "cmd", "flag", "value");
                return new SetFlagCommand(RequiredString(e, "flag", path), OptionalBool(e, "value", path) ?? true);
            case "if_flag":
                CheckKeys(e, path, "cmd", "flag", "value", "then", "else");
                return new IfFlagCommand(
                    RequiredString(e, "flag", path),
                    OptionalBool(e, "value", path) ?? true,
                    ReadBlock(e, "then", $"{path}.then", required: false),
                    ReadBlock(e, "else", $"{path}.else", required: false));
            case "background":
                CheckKeys(e, path, "cmd", "image");
                return new BackgroundCommand(OptionalString(e, "image", path));
            case "wait":
                CheckKeys(e, path, "cmd", "seconds");
                return new WaitCommand(ReadSeconds(e, path));
            case "end":
                CheckKeys(e, path, "cmd");
                return new EndCommand();
            default:
                throw Error(path, $"has unknown cmd \"{cmd}\". Known: enter, exit, expression, say, choice, set_flag, if_flag, background, wait, end");
        }
    }

    private static List<ChoiceOption> ReadOptions(JsonElement e, string path)
    {
        if (!TryGet(e, "options", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            throw Error(path, "needs an \"options\" array");
        }

        var options = new List<ChoiceOption>();
        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            var optionPath = $"{path}.options[{index}]";
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw Error(optionPath, "must be an object with \"text\"");
            }

            CheckKeys(item, optionPath, "text", "then", "ifFlag", "ifValue");
            options.Add(new ChoiceOption(
                RequiredString(item, "text", optionPath),
                ReadBlock(item, "then", $"{optionPath}.then", required: false),
                OptionalString(item, "ifFlag", optionPath),
                OptionalBool(item, "ifValue", optionPath) ?? true));
            index++;
        }

        if (options.Count == 0)
        {
            throw Error(path, "needs at least one option");
        }

        return options;
    }

    private static StageSlot ReadSlot(JsonElement e, string path)
    {
        var slot = RequiredString(e, "slot", path);
        return slot switch
        {
            "left" => StageSlot.Left,
            "center" => StageSlot.Center,
            "right" => StageSlot.Right,
            _ => throw Error(path, $"has unknown slot \"{slot}\". Use left, center or right"),
        };
    }

    private static double ReadSeconds(JsonElement e, string path)
    {
        if (!TryGet(e, "seconds", out var value) || value.ValueKind != JsonValueKind.Number)
        {
            throw Error(path, "needs a number \"seconds\"");
        }

        var seconds = value.GetDouble();
        if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
        {
            throw Error(path, $"has \"seconds\" {seconds}, which must be 0 or more");
        }

        return seconds;
    }

    private static void CheckKeys(JsonElement e, string path, params string[] allowed)
    {
        foreach (var property in e.EnumerateObject())
        {
            if (Array.IndexOf(allowed, property.Name) < 0)
            {
                throw Error(path, $"has unknown field \"{property.Name}\". Allowed: {string.Join(", ", allowed)}");
            }
        }
    }

    private static bool TryGet(JsonElement e, string key, out JsonElement value) =>
        e.TryGetProperty(key, out value) && value.ValueKind != JsonValueKind.Null;

    private static string RequiredString(JsonElement e, string key, string path) =>
        OptionalString(e, key, path) ?? throw Error(path, $"needs a \"{key}\"");

    private static string? OptionalString(JsonElement e, string key, string path)
    {
        if (!TryGet(e, key, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw Error(path, $"has \"{key}\" that is not a string");
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw Error(path, $"has an empty \"{key}\"");
        }

        return text;
    }

    private static bool? OptionalBool(JsonElement e, string key, string path)
    {
        if (!TryGet(e, key, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw Error(path, $"has \"{key}\" that is not true or false"),
        };
    }

    private static InvalidDataException Error(string path, string message) => new($"Dialogue {path} {message}.");
}

# Dialogue scripts

A dialogue scene is a JSON file in `data/dialogue/`. Adding a scene needs no code. [`data/dialogue/example.json`](../data/dialogue/example.json) shows every command.

The scene UI (portraits, text box, typewriter) and NPC triggers are separate tickets. This page covers the script format and the runner.

## File format

```json
{
  "id": "elder_intro",
  "commands": [
    { "cmd": "enter", "character": "elder", "slot": "right" },
    { "cmd": "say", "speaker": "elder", "text": "Welcome, traveller." }
  ]
}
```

- `id` must match the file name (`elder_intro.json`). A test checks this for every file.
- `//` comments and trailing commas are allowed.
- Misspelled fields, unknown commands and wrong types fail when the script loads. The message says where, for example `Dialogue commands[2].options[0].then[1] needs a "text".`

## Commands

| `cmd` | Fields | What it does |
| --- | --- | --- |
| `enter` | `character`, `slot` (`left`, `center` or `right`), optional `expression` (default `neutral`) | Puts a character on stage. If they are already on stage, they move. Whoever stood in that slot leaves. |
| `exit` | optional `character` | Takes the character off stage. With no `character`, everyone leaves. |
| `expression` | `character`, `expression` | Changes the portrait of a character on stage. |
| `say` | optional `speaker`, `text` | Shows a line and waits for a click. No `speaker` means narration. The speaker doesn't have to be on stage. |
| `choice` | `options`: a list of `{ "text", "then", "ifFlag", "ifValue" }` | Shows the options and waits for a pick. Runs that option's `then` commands, then carries on after the choice. An option with `ifFlag` only shows when that flag equals `ifValue` (default `true`). If no option shows, the choice is skipped. |
| `set_flag` | `flag`, optional `value` (default `true`) | Sets a [dialogue flag](save.md#dialogue-flags). Flags are saved with the game. |
| `if_flag` | `flag`, optional `value` (default `true`), optional `then`, optional `else` | Runs `then` if the flag equals `value`, otherwise `else`, then carries on. A flag that was never set is `false`. |
| `background` | optional `image` | Changes the background. No `image` clears it. |
| `wait` | `seconds` (0 or more) | Pauses. |
| `end` | | Ends the scene at once, even from inside a branch. |

`then` and `else` blocks can hold any commands, including more choices and branches.

## Running a script

```csharp
var script = DialogueScript.FromJson(json);
var runner = new DialogueRunner(script, SaveService.Flags);

var next = runner.Advance();
```

`Advance` handles flags and branches itself and returns the next `DialogueEvent` for the UI:

| Event | The UI should |
| --- | --- |
| `CharacterEntered`, `CharacterExited`, `ExpressionChanged`, `BackgroundChanged` | Apply it and call `Advance` again (`Instant` is true) |
| `LineShown` | Show the line, call `Advance` on click |
| `WaitRequested` | Wait `Seconds`, then call `Advance` |
| `ChoiceShown` | Show `Options`, then call `Choose(index)` and `Advance`. The index counts only the options shown |
| `DialogueEnded` | Close the scene. `Advance` keeps returning it |

`runner.Stage` and `runner.Background` hold the current stage, for example to redraw it.

Calling `Advance` while a choice is waiting throws `InvalidOperationException`. Changing the expression of, or removing, a character who isn't on stage throws `InvalidDataException` naming the script.

## Pieces

| Piece | Where | What it does |
| --- | --- | --- |
| `DialogueScript` | `scripts/core/Dialogue/` | Reads and validates a script |
| `DialogueCommand` records | `scripts/core/Dialogue/DialogueCommand.cs` | One record per command |
| `DialogueRunner` | `scripts/core/Dialogue/` | Steps through a script, tracks the stage, reads and writes flags |
| `DialogueEvent` records | `scripts/core/Dialogue/DialogueEvent.cs` | What the UI should do next |

They have no Godot dependency and are tested in `tests/Ragna.Core.Tests/Dialogue/`.

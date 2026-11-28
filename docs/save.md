# Saving and loading

Press **F5** to save and **F9** to load. There is one save, a JSON file at `user://save.json`. On Linux that is `~/.local/share/godot/app_userdata/Ragna/save.json`. The HUD log says `Game saved.`, `Game loaded.` or why it couldn't do either.

## What is saved

| Part | Fields |
| --- | --- |
| Player | Level, EXP, unspent stat points, the six base stats, current HP and SP, position |
| Inventory | Grid size, then each non-empty slot with its item id and quantity |
| Dialogue flags | Every flag in `SaveService.Flags`, by name |

Monsters, items on the ground and the camera are not saved.

## Pieces

| Piece | Where | What it does |
| --- | --- | --- |
| `SaveData`, `PlayerSave`, `InventorySave`, `SlotSave` | `scripts/core/Save/SaveData.cs` | The save format |
| `SaveSerializer` | `scripts/core/Save/` | Writes and reads the JSON. Rejects files that aren't saves and saves from a newer version |
| `SaveMapper` | `scripts/core/Save/` | Copies stats, HP, SP and the inventory into a save and back |
| `DialogueFlags` | `scripts/core/Save/` | Named true/false flags for dialogue and story triggers |
| `SaveService` | `scripts/gameplay/Save/`, the `SaveService` autoload | Finds the player, writes the file and applies a loaded save |

The core pieces have no Godot dependency. They are tested in `tests/Ragna.Core.Tests/Save/`, including a JSON round trip of every part.

## Rules

- **Loading changes the game in place.** The player's stats, HP and SP objects stay the same, so the HUD keeps updating. The player stops walking and attacking and appears at the saved position.
- **A dead player loads with at least 1 HP.**
- **Bad stats:** if a saved stat is out of range for `data/stats/stat_table.json`, loading fails and nothing changes.
- **Items:** an item id that is no longer in `data/items/items.json` is dropped. A stack whose slot no longer exists, or that is bigger than the item's `maxStack`, has the extra added wherever it fits. Anything that doesn't fit is dropped. Each of these is printed as a warning.
- **Safe writes:** the save is written to `save.json.tmp`, then renamed over `save.json`, so a crash while saving keeps the old save.
- **Versions:** each save has a `version`. This build writes and reads version 1. When the format changes, raise `SaveData.CurrentVersion` and upgrade older saves in `SaveSerializer.FromJson`.

## Dialogue flags

Reach the flags from any node through the autoload:

```csharp
var flags = SaveService.Instance!.Flags;
flags.Set("met_elder");
if (flags.Get("met_elder")) { /* ... */ }
```

A flag never set reads as false. Names are case-sensitive.

## From code

```csharp
SaveService.Instance!.Save();   // returns false and raises SaveFailed on error
SaveService.Instance!.Load();   // returns false and raises LoadFailed on error; nothing changes
bool any = SaveService.HasSave;
```

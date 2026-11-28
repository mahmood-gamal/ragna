# Item drops and pick-up

When a monster dies it rolls its drop table. Each item that drops falls out of the body in a short arc and lands on the ground nearby, with its name above it. Left-click an item to walk over and pick it up.

The rules live in `Ragna.Core.Items` (`scripts/core/Items/`), with no Godot dependency, and are tested in `tests/Ragna.Core.Tests/Items/`.

| Piece | Where | What it does |
| --- | --- | --- |
| `ItemDefinition`, `ItemDatabase` | core | One item type, and all of them loaded and checked from `data/items/items.json` |
| `DropEntry`, `DropTable` | core | One line of a drop table, and the roll on death |
| `DropScatter` | core | Spreads several drops around the body so they don't stack on one spot |
| `PickupOrder` | core | Walk to a clicked item and pick it up in reach, or give up if the walk ends short |
| `GroundItem` | `scripts/gameplay/Items/`, `scenes/items/GroundItem.tscn` | An item on the ground: falls, shows its name, can be clicked and picked up |
| `LootDropper` | `scripts/gameplay/Items/` | Rolls a dead monster's drops and spawns the `GroundItem`s |
| `PlayerPickup` | `scripts/gameplay/Items/`, the `Pickup` node in `Player.tscn` | Click-to-pick-up for the player |
| `ItemData` | `scripts/gameplay/Items/` | Loads `items.json` once for the game |

## Item data

`data/items/items.json` defines the item types. Comments and trailing commas are allowed.

| Key | Meaning |
| --- | --- |
| `id` | Key used by drop tables, for example `slime_gel`. Must be unique |
| `name` | Name shown to the player |
| `description` | One line for tooltips in the inventory |
| `tint` | Colour of the placeholder ground sprite (and the inventory slot), `#rrggbb` |
| `maxStack` | Most of this item one inventory slot holds. Default 99. 1 means it doesn't stack |

## Drop tables

Each monster in `data/monsters/monsters.json` has a `drops` list. Every line is rolled on its own, so one kill can drop several items, or none.

```json
"drops": [
  { "item": "slime_gel", "chance": 0.7, "min": 1, "max": 2 },
  { "item": "green_herb", "chance": 0.25 }
]
```

| Key | Meaning |
| --- | --- |
| `item` | An `id` from `items.json` |
| `chance` | From 0 (never) to 1 (always). Default 1 |
| `min`, `max` | How many drop when it drops, picked evenly from `min` to `max`. Default 1. `min` is at least 1 and `max` at least `min` |

A stack (for example 2 Slime Gel) lands as one ground item labelled `Slime Gel x2`.

The shipped tables:

| Monster | Drops |
| --- | --- |
| Slime | Slime Gel 70% (1–2), Green Herb 25% |
| Boar | Boar Hide 50%, Boar Tusk 35%, Raw Meat 40% (1–2), Green Herb 15% |

`ItemDatabaseTests` loads both shipped files and checks that every drop names a known item, so a typo fails `dotnet test`. In the game, an unknown item is skipped with a warning.

## On the ground

- Items land 0.35–0.9 m from the body, spread evenly around it, snapped to the navmesh (so the player can reach them) and then onto the terrain.
- They sit on the `items` physics layer (layer 4).
- An item nobody picks up disappears after `Lifetime` (120 s by default, set on `GroundItem.tscn`).

## Picking up

Left-click an item: the player walks to it and picks it up once within `Reach` (0.9 m, set on the `Pickup` node). Clicking an item also stops attacking. Clicking the ground or a monster cancels the walk to the item. If the walk ends before the item is in reach, the player gives up.

Terrain and monsters in front of an item block the click. The `Pickup` node comes after `Combat` under the player, so it sees clicks first.

Picked-up items go into the player's inventory (see [inventory.md](inventory.md)). If the stack doesn't fit, the item stays on the ground. `PlayerPickup.PickedUp(item, quantity)` fires after each pick-up.
